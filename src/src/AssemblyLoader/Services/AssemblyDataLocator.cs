using System.Collections.Generic;
using System.Reflection;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Services
{
    /// <summary>
    /// Locates assembly DLLs based on context and hints
    /// </summary>
    public sealed class AssemblyDataLocator : IAssemblyDataLocator
    {
        private readonly ILazyModuleHintsProvider _lazyModuleNamesProvider;

        /// <inheritdoc/>
        public AssemblyDataLocator(
            ILazyModuleHintsProvider lazyModuleProvider)
        {
            _lazyModuleNamesProvider = lazyModuleProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetFindPaths(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            foreach (var module in _lazyModuleNamesProvider.ModuleNameHints)
            {
                yield return $"_content/{module}/_lazy";
            }

            yield return $"_content/{assemblyName}/_lazy";
            yield return $"_framework/_bin";

            //List<AssemblyLoaderContext> branches = new List<AssemblyLoaderContext> { context };
            //AssemblyLoaderContext contextRoot = context;

            //while (contextRoot.Parent != null)
            //{
            //    contextRoot = contextRoot.Parent;
            //    branches.Add(contextRoot);
            //}

            //branches.Reverse();
            //branches.RemoveAt(0);

            //foreach (var branch in branches)
            //{
            //    yield return $"_content/{branch.AssemblyName.Name}/_lazy";
            //}
        }
    }
}
