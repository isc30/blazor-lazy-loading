using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Services
{
    public sealed class AssemblyDataProvider : IAssemblyDataProvider
    {
        private readonly IAssemblyDataLocator _assemblyDataLocator;
        private readonly IContentFileReader _contentFileReader;

        public AssemblyDataProvider(
            IAssemblyDataLocator assemblyDataLocator,
            IContentFileReader contentFileReader)
        {
            _assemblyDataLocator = assemblyDataLocator;
            _contentFileReader = contentFileReader;
        }

        public async Task<AssemblyData?> GetAssemblyDataAsync(AssemblyName assemblyName, AssemblyLoaderContext context)
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
            Task<byte[]?> dll = _contentFileReader.ReadBytesOrNullAsync(basePath, $"{assemblyName.Name}.dll");

            Task<byte[]?> pdb = Debugger.IsAttached
                ? _contentFileReader.ReadBytesOrNullAsync(basePath, $"{assemblyName.Name}.pdb")
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
    }
}
