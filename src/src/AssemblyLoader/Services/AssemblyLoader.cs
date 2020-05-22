using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Comparers;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Services
{
    public sealed class AssemblyLoader : IAssemblyLoader
    {
        private readonly IAssemblyDataProvider _assemblyDataProvider;

        private IAssemblyLoadContext? _assemblyLoadContext;
        private ConcurrentDictionary<AssemblyName, Task<Assembly?>> _loadingAssemblies;
        private List<Func<Assembly, Task>> _onAssemblyLoad = new List<Func<Assembly, Task>>();

        public AssemblyLoader(
            IAssemblyDataProvider assemblyDataProvider,
            IAssemblyLoadContext assemblyLoadContext)
        {
            _assemblyDataProvider = assemblyDataProvider;
            _assemblyLoadContext = assemblyLoadContext;

            _loadingAssemblies = new ConcurrentDictionary<AssemblyName, Task<Assembly?>>(
                AssemblyByNameAndVersionComparer.Default);
        }

        public void SubscribeOnAssemblyLoad(Func<Assembly, Task> callback)
        {
            _onAssemblyLoad.Add(callback);
        }

        public void UnsubscribeOnAssemblyLoad(Func<Assembly, Task> callback)
        {
            _onAssemblyLoad.Remove(callback);
        }

        public Assembly? GetLoadedAssemblyByName(AssemblyName assemblyName)
        {
            IAssemblyComparer comparer = GetAssemblyNameComparer(assemblyName);

            if (TryGetAlreadyLoadedAssembly(assemblyName, comparer, out var alreadyLoadedAssembly))
            {
                return alreadyLoadedAssembly;
            }

            return null;
        }

        public Task<Assembly?> LoadAssemblyByNameAsync(AssemblyName assemblyName)
        {
            return LoadAssemblyByNameAsync(assemblyName, null);
        }

        private async Task<Assembly?> LoadAssemblyByNameAsync(
            AssemblyName assemblyName,
            AssemblyLoaderContext? context)
        {
            if (_assemblyLoadContext == null)
            {
                return null;
            }

            IAssemblyComparer comparer = GetAssemblyNameComparer(assemblyName);

            if (TryGetAlreadyLoadedAssembly(assemblyName, comparer, out var alreadyLoadedAssembly))
            {
                return alreadyLoadedAssembly;
            }

            if (TryGetAlreadyLoadingAssembly(assemblyName, comparer, out var assemblyLoadingTask))
            {
                Debug.WriteLine($"Waiting for Loading Assembly '{assemblyName}'");

                return await assemblyLoadingTask!.ConfigureAwait(false);
            }

            AssemblyLoaderContext contextScope = context == null
                ? new AssemblyLoaderContext(assemblyName)
                : context.NewScope(assemblyName);

            Assembly? assembly = await PerformAssemblyLoad(assemblyName, comparer, contextScope).ConfigureAwait(false);

            return assembly;
        }

        private bool TryGetAlreadyLoadingAssembly(
            AssemblyName assemblyName,
            IAssemblyComparer comparer,
            out Task<Assembly?>? assemblyLoadingTask)
        {
            var assemblyLoadingEntry = _loadingAssemblies
                .FirstOrDefault(kv => comparer.Equals(assemblyName, kv.Key));

            if (assemblyLoadingEntry.Key == null)
            {
                assemblyLoadingTask = null;
                return false;
            }

            assemblyLoadingTask = assemblyLoadingEntry.Value;
            return true;
        }

        private bool TryGetAlreadyLoadedAssembly(
            AssemblyName assemblyName,
            IAssemblyComparer comparer,
            out Assembly? assembly)
        {
            if (_assemblyLoadContext == null)
            {
                assembly = null;
                return false;
            }

            Assembly? loadedAssembly = _assemblyLoadContext.AllAssemblies
                .FirstOrDefault(a => comparer.Equals(assemblyName, a.GetName()));

            assembly = loadedAssembly;
            return loadedAssembly != null;
        }

        private async Task<Assembly?> PerformAssemblyLoad(
            AssemblyName assemblyName,
            IAssemblyComparer comparer,
            AssemblyLoaderContext context)
        {
            var assemblyLoadingTaskSource = new TaskCompletionSource<Assembly?>();

            if (!TryActionRepeteadly(() => _loadingAssemblies.TryAdd(assemblyName, assemblyLoadingTaskSource.Task), 5))
            {
                throw new InvalidOperationException($"Unable to Load Assembly '{assemblyName}': Concurrency error (adding)");
            }

            Debug.WriteLine($"Loading Assembly: '{assemblyName}'");

            Assembly? assembly = await ResolveAssembly(assemblyName, comparer, context).ConfigureAwait(false);

            if (assembly != null)
            {
                Debug.WriteLine($"Loaded Assembly: '{assemblyName}'");

                foreach (var assemblyLoadCallback in _onAssemblyLoad)
                {
                    await assemblyLoadCallback.Invoke(assembly).ConfigureAwait(false);
                }
            }
            else
            {
                Debug.WriteLine($"Assembly '{assemblyName}' failed to load");
            }

            assemblyLoadingTaskSource.SetResult(assembly);

            if (!TryActionRepeteadly(() => _loadingAssemblies.TryRemove(assemblyName, out var _), 5))
            {
                throw new InvalidOperationException($"Unable to Load Assembly '{assemblyName}': Concurrency error (removing)");
            }

            return assembly;
        }

        private async Task<Assembly?> ResolveAssembly(
            AssemblyName assemblyName,
            IAssemblyComparer comparer,
            AssemblyLoaderContext context)
        {
            if (_assemblyLoadContext == null)
            {
                return null;
            }

            // Try loading the assembly by name (this works when the assembly is part of the bootloader, but never used explicitly)
            Assembly? assembly = _assemblyLoadContext.Load(assemblyName);

            if (assembly != null)
            {
                return assembly;
            }

            AssemblyData? data = await _assemblyDataProvider.GetAssemblyDataAsync(assemblyName, context).ConfigureAwait(false);

            if (data == null)
            {
                Debug.WriteLine($"Failed to resolve Assembly: '{assemblyName}'");
                return null;
            }

            var dependencies = await ResolveDependencies(assemblyName, data, context).ConfigureAwait(false);

            if (dependencies == null)
            {
                return null;
            }

            return _assemblyLoadContext.Load(data);
        }

        private async Task<ICollection<Assembly>?> ResolveDependencies(
            AssemblyName assemblyName,
            AssemblyData data,
            AssemblyLoaderContext context)
        {
            var dependencies = GetAssemblyDependencies(data, context);
            var loadDependenciesTasks = dependencies
                .Select(async n => new
                {
                    Name = n,
                    Assembly = await LoadAssemblyByNameAsync(n, context).ConfigureAwait(false),
                })
                .ToList();

            await Task.WhenAll(loadDependenciesTasks).ConfigureAwait(false);

            var resolvedAssemblies = loadDependenciesTasks
                .Select(t => t.Result)
                .ToList();

            var erroredAssemblies = resolvedAssemblies
                .Where(a => a.Assembly == null)
                .ToList();

            foreach (var missingDependency in erroredAssemblies)
            {
                Debug.WriteLine($"Dependency load failed: '{missingDependency.Name}' required by '{assemblyName}'");
            }

            if (erroredAssemblies.Any())
            {
                return null;
            }

            return resolvedAssemblies
                .Select(a => a.Assembly!)
                .ToList();
        }

        private ICollection<AssemblyName> GetAssemblyDependencies(
            AssemblyData assemblyData,
            AssemblyLoaderContext context)
        {
            if (_assemblyLoadContext == null || assemblyData.DllBytes == null)
            {
                return Array.Empty<AssemblyName>();
            }

            using MemoryStream dllStream = new MemoryStream(assemblyData.DllBytes);
            using PEReader peReader = new PEReader(dllStream);
            MetadataReader mdReader = peReader.GetMetadataReader(MetadataReaderOptions.None);

            // TODO: Throw error if the loaded assembly doesn't match the version requested

            return mdReader.AssemblyReferences
                .Select(x => mdReader.GetAssemblyReference(x).GetAssemblyName())
                .Except(_assemblyLoadContext.AllAssemblies.Select(a => a.GetName()), AssemblyByNameAndVersionComparer.Default)
                .ToList();
        }

        private static IAssemblyComparer GetAssemblyNameComparer(AssemblyName assemblyName)
        {
            //return assemblyName.Version == null || assemblyName.Version.ToString() == "0.0.0.0"
            //    ? (IAssemblyComparer)AssemblyByNameComparer.Default
            //    : AssemblyByNameAndVersionComparer.Default;

            // Until we can properly version DLL names, versions will not be taken into consideration
            return AssemblyByNameComparer.Default;
        }

        private static bool TryActionRepeteadly(Func<bool> action, int retry)
        {
            for (var i = 0; i < retry; ++i)
            {
                if (action.Invoke())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
