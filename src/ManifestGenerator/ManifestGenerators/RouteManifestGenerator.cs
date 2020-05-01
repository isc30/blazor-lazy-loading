using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorLazyLoading.ManifestGenerators
{
    public sealed class RouteManifestGenerator : IManifestGenerator
    {
        public Dictionary<string, object>? GenerateManifest(Assembly assembly)
        {
            var componentTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.FullName == "Microsoft.AspNetCore.Components.IComponent"));

            var routes = componentTypes.SelectMany(t => t.GetCustomAttributesData()
                .Where(a => a.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
                .Select(a => new RouteManifest((string)a.ConstructorArguments[0].Value, t.FullName)))
                .ToList();

            if (!routes.Any())
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                { "Routes", routes }
            };
        }

        private sealed class RouteManifest
        {
            public string Route { get; }

            public string TypeFullName { get; }

            public RouteManifest(
                string route,
                string typeFullName)
            {
                Route = route;
                TypeFullName = typeFullName;
            }
        }
    }
}
