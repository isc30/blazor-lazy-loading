using System.Collections.Generic;
using System.Reflection;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    /// <summary>
    /// Locates assembly DLLs based on context and hints
    /// </summary>
    public interface IAssemblyDataLocator
    {
        /// <summary>
        /// Returns a list of possible locations from where the assembly data can be retrieved
        /// </summary>
        public IEnumerable<AssemblyLocation> GetFindPaths(AssemblyName assemblyName, AssemblyLoaderContext context);
    }
}
