using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    /// <summary>
    /// Returns the AssemblyData (dll + pdb) based on AssemblyName and Context
    /// </summary>
    public interface IAssemblyDataProvider
    {
        /// <summary>
        /// Returns the AssemblyData (dll + pdb) based on AssemblyName and Context
        /// </summary>
        Task<AssemblyData?> GetAssemblyDataAsync(AssemblyName assemblyName, AssemblyLoaderContext context);
    }
}
