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
        private readonly IAssemblyDataLocator _assemblyDataLocator;
        private readonly IFileProvider _fileProvider;

        public FileProviderAssemblyDataProvider(
            IAssemblyDataLocator assemblyDataLocator,
            IFileProvider fileProvider)
        {
            _assemblyDataLocator = assemblyDataLocator;
            _fileProvider = fileProvider;
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
}
