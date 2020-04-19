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
        private readonly IAssemblyDataLocator _assemblyDataLocator;
        private readonly HttpClient _httpClient;

        public NetworkAssemblyDataProvider(
            IAssemblyDataLocator assemblyDataLocator,
            HttpClient httpClient)
        {
            _assemblyDataLocator = assemblyDataLocator;
            _httpClient = httpClient;
        }

        public async Task<AssemblyData?> GetAssemblyDataAsync(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            var paths = _assemblyDataLocator.GetFindPaths(assemblyName, context);

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
}
