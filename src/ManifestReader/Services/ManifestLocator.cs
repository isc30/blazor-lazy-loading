using System.Collections.Generic;
using System.Linq;
using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Services
{
    public sealed class ManifestLocator : IManifestLocator
    {
        private readonly ILazyModuleHintsProvider _moduleHintsProvider;

        public ManifestLocator(
            ILazyModuleHintsProvider moduleHintsProvider)
        {
            _moduleHintsProvider = moduleHintsProvider;
        }

        public IEnumerable<string> GetManifestPaths()
        {
            return _moduleHintsProvider.ModuleNameHints
                .Select(m => $"_content/{m}/_lazy.json");
        }
    }
}
