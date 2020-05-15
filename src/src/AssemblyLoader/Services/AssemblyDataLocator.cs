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
        public IEnumerable<AssemblyLocation> GetFindPaths(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            foreach (var module in _lazyModuleNamesProvider.ModuleNameHints)
            {
                yield return CreateAssemblyLocation($"_content/{module}/_lazy", assemblyName);
            }

            yield return CreateAssemblyLocation($"_content/{assemblyName}/_lazy", assemblyName);
            yield return CreateAssemblyLocation($"_framework/_bin", assemblyName);
        }

        private static AssemblyLocation CreateAssemblyLocation(string basePath, AssemblyName assembly)
            => new AssemblyLocation(
                $"{basePath}/{assembly.Name}.dll",
                $"{basePath}/{assembly.Name}.pdb");
    }
}
