using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Models;
using Microsoft.Extensions.FileProviders;

namespace BlazorLazyLoading.Server
{
    public sealed class FileProviderAssemblyDataProvider : IAssemblyDataProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly NetworkDependencyPathFinder _basePathResolver;

        public FileProviderAssemblyDataProvider(
            IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
            _basePathResolver = new NetworkDependencyPathFinder();
        }

        public async Task<AssemblyData?> GetAssemblyDataAsync(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            // TODO: strategy to get possible basePaths based on the AssemblyName and Context
            var paths = _basePathResolver.GetFindPaths(assemblyName, context);

            foreach (var path in paths)
            {
                AssemblyData? data = await GetAssemblyDataAsync(assemblyName, path).ConfigureAwait(false);

                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }

        private Task<AssemblyData?> GetAssemblyDataAsync(
            AssemblyName assemblyName,
            string basePath)
        {
            var dllBytes = FetchBytesOrNull(basePath, $"{assemblyName.Name}.dll");

            var pdbBytes = Debugger.IsAttached
                ? FetchBytesOrNull(basePath, $"{assemblyName.Name}.pdb")
                : null;

            if (dllBytes == null)
            {
                return Task.FromResult<AssemblyData?>(null);
            }

            return Task.FromResult<AssemblyData?>(
                new AssemblyData(dllBytes, pdbBytes));
        }

        private byte[]? FetchBytesOrNull(string basePath, string fileName)
        {
            try
            {
                using var fileStream = _fileProvider.GetFileInfo($"{basePath}/{fileName}").CreateReadStream();
                using var memoryStream = new MemoryStream();

                fileStream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }

    public class NetworkDependencyPathFinder
    {
        public IEnumerable<string> GetFindPaths(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            List<AssemblyLoaderContext> branches = new List<AssemblyLoaderContext> { context };
            AssemblyLoaderContext contextRoot = context;

            while (contextRoot.Parent != null)
            {
                contextRoot = contextRoot.Parent;
                branches.Add(contextRoot);
            }

            branches.Reverse();
            branches.RemoveAt(0);

            yield return $"_content/{contextRoot.AssemblyName.Name}/_lazy";
            yield return $"_framework/_bin";

            foreach (var branch in branches)
            {
                yield return $"_content/{branch.AssemblyName.Name}/_lazy";
            }
        }
    }
}
