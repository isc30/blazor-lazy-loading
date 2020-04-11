using System;
using System.Collections.Generic;
using System.Reflection;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyLoadContext : IDisposable
    {
        ICollection<Assembly> AllAssemblies { get; }

        ICollection<Assembly> OwnAssemblies { get; }

        Assembly? Load(AssemblyData assemblyData);
    }
}
