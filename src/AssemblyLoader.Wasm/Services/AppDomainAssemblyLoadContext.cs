using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Comparers;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Wasm.Services
{
    public sealed class AppDomainAssemblyLoadContext : IAssemblyLoadContext
    {
        private readonly object _domainLock = new object();
        private readonly AppDomain _baseDomain;
        private AppDomain? _domain;

        public AppDomainAssemblyLoadContext(string name)
        {
            _baseDomain = AppDomain.CurrentDomain;

            try
            {
                _domain = AppDomain.CreateDomain(name);
            }
            catch
            {
                _domain = _baseDomain;
            }
        }

        public ICollection<Assembly> OwnAssemblies
        {
            get
            {
                if (_domain == null
                    || _domain == _baseDomain)
                {
                    return Array.Empty<Assembly>();
                }

                return _domain.GetAssemblies().ToList();
            }
        }

        public ICollection<Assembly> AllAssemblies
        { 
            get
            {
                return OwnAssemblies
                    .Concat(_baseDomain.GetAssemblies())
                    .Distinct(AssemblyByNameAndVersionComparer.Default)
                    .ToList();
            }
        }

        public void Dispose()
        {
            lock (_domainLock)
            {
                if (_domain == null)
                {
                    return;
                }

                if (_domain == _baseDomain)
                {
                    Debug.WriteLine("Cannot dispose the global AppDomain");
                }
                else
                {
                    AppDomain.Unload(_domain);
                }

                _domain = null;
            }
        }

        public Assembly? Load(AssemblyData assemblyData)
        {
            try
            {
                lock (_domainLock)
                {
                    if (_domain == null)
                    {
                        return null;
                    }

                    if (assemblyData.PdbBytes != null)
                    {
                        return _domain.Load(
                            assemblyData.DllBytes,
                            assemblyData.PdbBytes);
                    }

                    return _domain.Load(assemblyData.DllBytes);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
