using System.Collections.Generic;
using System.Reflection;

namespace BlazorLazyLoading.Comparers
{
    public interface IAssemblyComparer
        : IEqualityComparer<Assembly>
        , IEqualityComparer<AssemblyName>
    {
    }
}
