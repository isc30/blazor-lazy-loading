using System.Collections.Generic;

namespace BlazorLazyLoading.Abstractions
{
    public interface ILazyModuleHintsProvider
    {
        IEnumerable<string> ModuleNameHints { get; }
    }
}
