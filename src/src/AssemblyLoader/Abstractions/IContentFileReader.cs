using System.Threading.Tasks;

namespace BlazorLazyLoading.Abstractions
{
    public interface IContentFileReader
    {
        Task<byte[]?> ReadModuleBytesOrNullAsync(string moduleName, string fileName);

        Task<byte[]?> ReadBytesOrNullAsync(string basePath, string fileName);

        Task<byte[]?> ReadBytesOrNullAsync(string filePath);
    }
}
