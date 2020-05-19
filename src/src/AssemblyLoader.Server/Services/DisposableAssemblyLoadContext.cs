using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Comparers;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Server.Services
{
    public sealed class DisposableAssemblyLoadContext : IAssemblyLoadContext
    {
        private readonly object _assemblyLoadContextLock = new object();
        private readonly bool _canBeUnloaded;

        private AssemblyLoadContext? _assemblyLoadContext;

        public ICollection<Assembly> OwnAssemblies
        {
            get
            {
                if (_assemblyLoadContext == null)
                {
                    return Array.Empty<Assembly>();
                }

                return _assemblyLoadContext.Assemblies.ToList();
            }
        }

        public ICollection<Assembly> AllAssemblies
        { 
            get
            {
                return OwnAssemblies
                    .Concat(AssemblyLoadContext.Default.Assemblies)
                    .Distinct(AssemblyByNameAndVersionComparer.Default)
                    .ToList();
            }
        }

        public DisposableAssemblyLoadContext(
            string name)
        {
            // visual studio crashes randomly when unloading assemblies with the debugger attached
            // https://github.com/dotnet/runtime/issues/535
            _canBeUnloaded = !Debugger.IsAttached;

            _assemblyLoadContext = new AssemblyLoadContext(
                name: name,
                isCollectible: _canBeUnloaded);
        }

        public Assembly? Load(AssemblyData assemblyData)
        {
            try
            {
                lock (_assemblyLoadContextLock)
                {
                    if (_assemblyLoadContext == null)
                    {
                        return null;
                    }

                    if (assemblyData.PdbBytes != null)
                    {
                        return _assemblyLoadContext.LoadFromStream(
                            new MemoryStream(assemblyData.DllBytes),
                            new MemoryStream(assemblyData.PdbBytes));
                    }

                    return _assemblyLoadContext.LoadFromStream(
                        new MemoryStream(assemblyData.DllBytes));
                }
            }
            catch { }

            return null;
        }

        public Assembly? Load(AssemblyName assemblyName)
        {
            try
            {
                lock (_assemblyLoadContextLock)
                {
                    if (_assemblyLoadContext == null)
                    {
                        return null;
                    }

                    return _assemblyLoadContext.LoadFromAssemblyName(assemblyName);
                }
            }
            catch { }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            try
            {
                lock (_assemblyLoadContextLock)
                {
                    if (!_canBeUnloaded || _assemblyLoadContext == null)
                    {
                        _assemblyLoadContext = null;
                        return;
                    }

                    // Unloading will not occur while there are references to the AssemblyLoadContext
                    WeakReference alc = new WeakReference(_assemblyLoadContext);
                    _assemblyLoadContext = null;

                    ((AssemblyLoadContext?)alc.Target)?.Unload();
                }
            }
            catch
            {
            }
        }
    }
}
