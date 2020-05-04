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
        /// Returns a list of possible paths to look for the assembly data (dll)
        /// </summary>
        public IEnumerable<string> GetFindPaths(AssemblyName assemblyName, AssemblyLoaderContext context);
    }
}
