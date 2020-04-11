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
    public sealed class AssemblyLoader : IAssemblyLoader, IDisposable
    {
        private readonly IAssemblyDataProvider _assemblyDataProvider;

        private IAssemblyLoadContext? _assemblyLoadContext;
        private ConcurrentDictionary<AssemblyName, Task<Assembly?>> _loadingAssemblies;

        public AssemblyLoader(
            IAssemblyDataProvider assemblyDataProvider,
            IAssemblyLoadContextFactory assemblyLoadContextFactory)
        {
            _assemblyDataProvider = assemblyDataProvider;
            _assemblyLoadContext = assemblyLoadContextFactory.Create(Guid.NewGuid().ToString());

            _loadingAssemblies = new ConcurrentDictionary<AssemblyName, Task<Assembly?>>(
                AssemblyByNameAndVersionComparer.Default);
        }

        public void Dispose()
        {
            if (_assemblyLoadContext != null)
            {
                _assemblyLoadContext.Dispose();
                _assemblyLoadContext = null;
            }
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

            return await PerformAssemblyLoad(assemblyName, comparer, contextScope).ConfigureAwait(false);
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

            if (!_loadingAssemblies.TryAdd(assemblyName, assemblyLoadingTaskSource.Task))
            {
                throw new InvalidOperationException($"Unable to Load Assembly '{assemblyName}': Concurrency error");
            }

            Debug.WriteLine($"Loading Assembly: '{assemblyName}'");

            Assembly? assembly = await ResolveAssembly(assemblyName, comparer, context).ConfigureAwait(false);

            if (assembly != null)
            {
                Debug.WriteLine($"Loaded Assembly: '{assemblyName}'");
            }
            else
            {
                Debug.WriteLine($"Assembly '{assemblyName}' failed to load");
            }

            assemblyLoadingTaskSource.SetResult(assembly);
            _loadingAssemblies.Remove(assemblyName, out var _);

            return assembly;
        }

        private async Task<Assembly?> ResolveAssembly(
            AssemblyName assemblyName,
            IAssemblyComparer comparer,
            AssemblyLoaderContext context)
        {
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

            if (_assemblyLoadContext == null)
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
    }
}
