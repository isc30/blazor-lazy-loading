using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Wasm.Services
{
    public class NetworkAssemblyDataProvider : IAssemblyDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly NetworkDependencyPathFinder _basePathResolver;

        public NetworkAssemblyDataProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

        private async Task<AssemblyData?> GetAssemblyDataAsync(
            AssemblyName assemblyName,
            string basePath)
        {
            Task<byte[]?> dll = FetchBytesOrNull(basePath, $"{assemblyName.Name}.dll");

            Task<byte[]?> pdb = Debugger.IsAttached
                ? FetchBytesOrNull(basePath, $"{assemblyName.Name}.pdb")
                : Task.FromResult<byte[]?>(null);

            await Task.WhenAll(dll, pdb).ConfigureAwait(false);

            var dllBytes = dll.Result;
            var pdbBytes = pdb.Result;

            if (dllBytes == null)
            {
                return null;
            }

            return new AssemblyData(dllBytes, pdbBytes);
        }

        private async Task<byte[]?> FetchBytesOrNull(string basePath, string fileName)
        {
            try
            {
                return await _httpClient
                    .GetByteArrayAsync($"{basePath}/{fileName}")
                    .ConfigureAwait(false);
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
