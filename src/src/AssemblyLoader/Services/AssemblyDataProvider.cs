using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Comparers;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Services
{
    public sealed class AssemblyDataProvider : IAssemblyDataProvider
    {
        private readonly IAssemblyDataLocator _assemblyDataLocator;
        private readonly IContentFileReader _contentFileReader;

        private readonly ConcurrentDictionary<AssemblyName, AssemblyData?> _assemblyDataCache;

        public AssemblyDataProvider(
            IAssemblyDataLocator assemblyDataLocator,
            IContentFileReader contentFileReader)
        {
            _assemblyDataLocator = assemblyDataLocator;
            _contentFileReader = contentFileReader;
            _assemblyDataCache = new ConcurrentDictionary<AssemblyName, AssemblyData?>(AssemblyByNameAndVersionComparer.Default);
        }

        public async Task<AssemblyData?> GetAssemblyDataAsync(
            AssemblyName assemblyName,
            AssemblyLoaderContext context)
        {
            if (_assemblyDataCache.TryGetValue(assemblyName, out AssemblyData? data))
            {
                return data;
            }

            var paths = _assemblyDataLocator.GetFindPaths(assemblyName, context);

            foreach (var path in paths)
            {
                data = await GetAssemblyDataAsync(path).ConfigureAwait(false);

                if (data != null)
                {
                    _assemblyDataCache.TryAdd(assemblyName, data);

                    return data;
                }
            }

            return null;
        }

        private async Task<AssemblyData?> GetAssemblyDataAsync(AssemblyLocation location)
        {
            Task<byte[]?> dll = _contentFileReader.ReadBytesOrNullAsync(location.DllPath);

            Task<byte[]?> pdb = Debugger.IsAttached && location.PdbPath != null
                ? _contentFileReader.ReadBytesOrNullAsync(location.PdbPath)
                : Task.FromResult<byte[]?>(null);

            await Task.WhenAll(dll, pdb).ConfigureAwait(false);

            var dllBytes = dll.Result;
            var pdbBytes = pdb.Result;

            if (dllBytes == null)
            {
                return null;
            }

            return new AssemblyData(dllBytes, pdbBytes);
        }
    }
}
