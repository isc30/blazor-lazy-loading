using System.Collections.Generic;
using System.Reflection;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyDataLocator
    {
        /// <summary>
        /// Returns a list of possible paths where the assembly data is
        /// </summary>
        public IEnumerable<string> GetFindPaths(AssemblyName assemblyName, AssemblyLoaderContext context);
    }
}
