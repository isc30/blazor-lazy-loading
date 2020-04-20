using System.IO;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using Microsoft.Extensions.FileProviders;

namespace BlazorLazyLoading.Server.Services
{
    public sealed class FileProviderContentFileReader : IContentFileReader
    {
        private readonly IFileProvider _fileProvider;

        public FileProviderContentFileReader(
            IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        public Task<byte[]?> ReadBytesOrNullAsync(string basePath, string fileName)
        {
            basePath = basePath.TrimEnd('/');

            try
            {
                using var fileStream = _fileProvider.GetFileInfo($"{basePath}/{fileName}").CreateReadStream();
                using var memoryStream = new MemoryStream();

                fileStream.CopyTo(memoryStream);

                return Task.FromResult<byte[]?>(memoryStream.ToArray());
            }
            catch
            {
                return Task.FromResult<byte[]?>(null);
            }
        }

        public Task<byte[]?> ReadModuleBytesOrNullAsync(string moduleName, string fileName)
        {
            return ReadBytesOrNullAsync($"_content/{moduleName}", fileName);
        }
    }
}
