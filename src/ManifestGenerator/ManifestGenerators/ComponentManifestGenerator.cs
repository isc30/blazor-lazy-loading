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

            var components = new List<ComponentManifest>();

            foreach (var component in componentTypes)
            {
                var lazyNameAttribute = component.GetCustomAttributesData()
                    .SingleOrDefault(a => a.AttributeType.FullName == "BlazorLazyLoading.LazyNameAttribute");

                if (lazyNameAttribute == null)
                {
                    components.Add(new ComponentManifest(component.FullName, null));
                    continue;
                }

                var lazyName = (string)lazyNameAttribute.ConstructorArguments[0].Value;
                components.Add(new ComponentManifest(component.FullName, lazyName));
            }

            return new Dictionary<string, object>
            {
                { "Components", components }
            };
        }

        private sealed class ComponentManifest
        {
            public string TypeFullName { get; }

            public string? Name { get; }

            public ComponentManifest(
                string typeFullName,
                string? lazyName)
            {
                TypeFullName = typeFullName;
                Name = lazyName;
            }
        }
    }
}
