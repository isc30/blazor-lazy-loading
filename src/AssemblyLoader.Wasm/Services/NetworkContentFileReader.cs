using System.Net.Http;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Wasm.Services
{
    public sealed class NetworkContentFileReader : IContentFileReader
    {
        private readonly HttpClient _httpClient;

        public NetworkContentFileReader(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<byte[]?> ReadBytesOrNullAsync(string basePath, string fileName)
        {
            basePath = basePath.TrimEnd('/');

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

        public Task<byte[]?> ReadModuleBytesOrNullAsync(string moduleName, string fileName)
        {
            return ReadBytesOrNullAsync($"_content/{moduleName}", fileName);
        }
    }
}
