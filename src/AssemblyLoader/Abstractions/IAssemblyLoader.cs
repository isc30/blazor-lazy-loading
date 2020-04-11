using System.Reflection;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyLoader
    {
        Task<Assembly?> LoadAssemblyByNameAsync(AssemblyName assemblyName);
    }
}
