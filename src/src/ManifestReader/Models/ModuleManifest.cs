using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BlazorLazyLoading.Models
{
    public sealed class ModuleManifest
    {
        public string ModuleName { get; set; }

        public Dictionary<string, JToken> ManifestSections { get; set; }

        public ModuleManifest(
            string moduleName,
            JObject manifest)
        {
            ModuleName = moduleName;

            var x = new Dictionary<string, JToken>();

            foreach (var key in manifest)
            {
                x.Add(key.Key, key.Value);
            }

            ManifestSections = x;
        }
    }
}
