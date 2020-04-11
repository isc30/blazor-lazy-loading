using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyDataProvider
    {
        Task<AssemblyData?> GetAssemblyDataAsync(AssemblyName assemblyName, AssemblyLoaderContext context);
    }
}
