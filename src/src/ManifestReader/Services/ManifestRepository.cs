using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Extensions;
using BlazorLazyLoading.Models;
using Newtonsoft.Json.Linq;

namespace BlazorLazyLoading.Services
{
    public sealed class ManifestRepository : IManifestRepository
    {
        private readonly IManifestLocator _manifestLocator;
        private readonly IContentFileReader _fileReader;

        private Task? _fetching = null;
        private readonly ConcurrentDictionary<string, byte[]?> _loadedPaths = new ConcurrentDictionary<string, byte[]?>();
        private readonly ConcurrentDictionary<string, ModuleManifest> _loadedModules = new ConcurrentDictionary<string, ModuleManifest>();

        public ManifestRepository(
            IManifestLocator manifestLocator,
            IContentFileReader fileReader)
        {
            _manifestLocator = manifestLocator;
            _fileReader = fileReader;
        }

        public async Task<ICollection<ModuleManifest>> GetAllAsync()
        {
            await FetchAllManifests().ConfigureAwait(false);

            return _loadedModules.Values.ToList();
        }

        public async Task<ModuleManifest?> GetByModuleNameAsync(string moduleName)
        {
            if (_loadedModules.TryGetValue(moduleName, out var manifest))
            {
                return manifest;
            }

            await FetchAllManifests().ConfigureAwait(false);

            if (_loadedModules.TryGetValue(moduleName, out manifest))
            {
                return manifest;
            }

            return null;
        }

        private async Task FetchAllManifests()
        {
            if (_fetching != null)
            {
                await _fetching.ConfigureAwait(false);
                return;
            }

            var manifestFetchingTaskSource = new TaskCompletionSource<byte>();
            _fetching = manifestFetchingTaskSource.Task;

            await ReadAllManifests().ConfigureAwait(false);

            manifestFetchingTaskSource.SetResult(0x90);
            _fetching = null;
        }

        private async Task ReadAllManifests()
        {
            var manifestDataTasks = new Dictionary<string, Task<byte[]?>>();
            var manifestPaths = _manifestLocator.GetManifestPaths().Except(_loadedPaths.Keys);

            foreach (var path in manifestPaths)
            {
                manifestDataTasks.Add(path, _fileReader.ReadBytesOrNullAsync(path));
            }

            await Task.WhenAll(manifestDataTasks.Values.ToArray()).ConfigureAwait(false);

            var pathData = manifestDataTasks
                .Select(i => new { Path = i.Key, Data = i.Value.Result })
                .ToList();

            foreach (var path in pathData)
            {
                _loadedPaths.TryAdd(path.Path, path.Data);
            }

            var manifestModels = pathData
                .Where(i => i.Data != null)
                .ToDictionary(i => i.Path, i => JObject.Parse(Encoding.UTF8.GetString(i.Data!)));

            foreach (var keyValue in manifestModels)
            {
                foreach (var module in keyValue.Value)
                {
                    if (module.Value.Type != JTokenType.Object)
                    {
                        continue;
                    }

                    _loadedModules.TryAdd(module.Key, new ModuleManifest(module.Key, (JObject)module.Value));
                }
            }
        }
    }
}
