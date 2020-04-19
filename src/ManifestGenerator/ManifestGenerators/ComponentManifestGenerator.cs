using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorLazyLoading.ManifestGenerators
{
    public sealed class ComponentManifestGenerator : IManifestGenerator
    {
        public Dictionary<string, object> GenerateManifest(Assembly assembly)
        {
            var componentTypes = assembly.GetTypes()
                .Where(t => t.BaseType.FullName == "Microsoft.AspNetCore.Components.ComponentBase");

            var components = componentTypes
                .Select(t => new ComponentManifest(t.FullName))
                .ToList();

            return new Dictionary<string, object>
            {
                { "Components", components }
            };
        }

        private sealed class ComponentManifest
        {
            public string TypeFullName { get; }

            public ComponentManifest(
                string typeFullName)
            {
                TypeFullName = typeFullName;
            }
        }
    }
}
