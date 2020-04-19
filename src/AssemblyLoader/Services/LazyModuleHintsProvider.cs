using System.Collections.Generic;
using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Services
{
    public class LazyModuleHintsProvider : ILazyModuleHintsProvider
    {
        public IEnumerable<string> ModuleNameHints { get; }

        public LazyModuleHintsProvider(
            IEnumerable<string> moduleNameHints)
        {
            ModuleNameHints = moduleNameHints;
        }
    }
}
