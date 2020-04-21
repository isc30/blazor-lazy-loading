using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Models;
using Newtonsoft.Json.Linq;

namespace BlazorLazyLoading.Services
{
    public sealed class ManifestRepository : IManifestRepository
    {
        private readonly IManifestLocator _manifestLocator;
        private readonly IContentFileReader _fileReader;

        private readonly ConcurrentDictionary<string, ModuleManifest> _loadedManifests;

        public ManifestRepository(
            IManifestLocator manifestLocator,
            IContentFileReader fileReader)
        {
            _manifestLocator = manifestLocator;
            _fileReader = fileReader;

            _loadedManifests = new ConcurrentDictionary<string, ModuleManifest>();
        }

        public async Task<ICollection<ModuleManifest>> GetAllAsync()
        {
            await ReadAllManifests().ConfigureAwait(false);

            return _loadedManifests.Values;
        }

        public async Task<ModuleManifest?> GetByModuleNameAsync(string moduleName)
        {
            if (_loadedManifests.TryGetValue(moduleName, out var manifest))
            {
                return manifest;
            }

            await ReadAllManifests().ConfigureAwait(false);

            return _loadedManifests.SingleOrDefault(m => m.Value.ModuleName == moduleName).Value;
        }

        private async Task ReadAllManifests()
        {
            var manifestPaths = _manifestLocator.GetManifestPaths().Except(_loadedManifests.Keys);
            var manifestDataTasks = new Dictionary<string, Task<byte[]?>>();

            foreach (var path in manifestPaths)
            {
                manifestDataTasks.Add(path, _fileReader.ReadBytesOrNullAsync(path));
            }

            await Task.WhenAll(manifestDataTasks.Values.ToArray()).ConfigureAwait(false);

            var manifestData = manifestDataTasks
                .Select(i => new { Path = i.Key, Data = i.Value.Result })
                .Where(i => i.Data != null)
                .ToDictionary(i => i.Path, i => JObject.Parse(Encoding.UTF8.GetString(i.Data!)));

            foreach (var keyValue in manifestData)
            {
                foreach (var module in keyValue.Value)
                {
                    if (module.Value.Type != JTokenType.Object)
                    {
                        continue;
                    }

                    _loadedManifests.TryAdd(keyValue.Key, new ModuleManifest(module.Key, (JObject)module.Value));
                }
            }
        }
    }
}
