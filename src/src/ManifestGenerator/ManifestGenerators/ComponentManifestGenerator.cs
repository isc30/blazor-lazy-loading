using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorLazyLoading.ManifestGenerators
{
    public sealed class ComponentManifestGenerator : IManifestGenerator
    {
        private readonly Logger _logger;

        public ComponentManifestGenerator(Logger logger)
        {
            _logger = logger;
        }

        public Dictionary<string, object>? GenerateManifest(Assembly assembly, MetadataLoadContext metadataLoadContext)
        {
            var types = assembly.GetTypes();
            var components = new List<ComponentManifest>();

            foreach (var type in types)
            {
                bool isComponent = true;

                // after net5, some assemblies crash when trying to enumerate their types :)
                try
                {
                    isComponent = type.GetInterface("Microsoft.AspNetCore.Components.IComponent", false) != null;
                }
                catch
                {
                }

                if (type.IsAbstract || !isComponent)
                {
                    continue;
                }

                var lazyNameAttribute = type.GetCustomAttributesData()
                    .SingleOrDefault(a => a.AttributeType.FullName == "BlazorLazyLoading.LazyNameAttribute");

                if (lazyNameAttribute == null)
                {
                    components.Add(new ComponentManifest(type.FullName, null));
                    continue;
                }

                var lazyName = (string)lazyNameAttribute.ConstructorArguments[0].Value;
                components.Add(new ComponentManifest(type.FullName, lazyName));
            }

            if (!components.Any())
            {
                return null;
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
