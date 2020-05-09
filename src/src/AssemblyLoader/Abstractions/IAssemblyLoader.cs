using System.Reflection;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyLoader
    {
        Assembly? GetLoadedAssemblyByName(AssemblyName assemblyName);

        Task<Assembly?> LoadAssemblyByNameAsync(AssemblyName assemblyName);
    }
}
