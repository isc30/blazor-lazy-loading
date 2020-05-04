using System.Collections.Generic;
using System.Linq;
using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Services
{
    /// <summary>
    /// Locates _lazy.json manifests based on hints
    /// </summary>
    public sealed class ManifestLocator : IManifestLocator
    {
        private readonly ILazyModuleHintsProvider _moduleHintsProvider;

        /// <inheritdoc/>
        public ManifestLocator(
            ILazyModuleHintsProvider moduleHintsProvider)
        {
            _moduleHintsProvider = moduleHintsProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetManifestPaths()
        {
            return _moduleHintsProvider.ModuleNameHints
                .Select(m => $"_content/{m}/_lazy.json");
        }
    }
}
