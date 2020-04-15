using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace BlazorLazyLoading
{
    public class LoadDLLInfo : Task
    {
        [Required]
        public string AssemblyName { get; set; } = string.Empty;

        [Required]
        public string[] AssemblyPaths { get; set; } = Array.Empty<string>();

        [Required]
        public string JsonManifestPath { get; set; } = string.Empty;

        public override bool Execute()
        {
            SanitizeInput();

            (var components, var routes) = ExtractAssemblyComponents();
            string json = GenerateJsonManifest(AssemblyName, components, routes);
            File.WriteAllText(JsonManifestPath, json);

            Info($"Lazy Module '{AssemblyName}' generated: {{ Components: {components.Count} }}, {{ Routes: {routes.Count} }}");

            return true;
        }

        private void SanitizeInput()
        {
            AssemblyPaths = AssemblyPaths.Select(p => p
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)).ToArray();
        }

        private IEnumerable<string> ResolveAvailableDlls()
        {
            var coreDlls = new[] { typeof(object).Assembly.Location };
            var runtimeDlls = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var moduleDlls = AssemblyPaths.SelectMany(p => Directory.GetFiles(p, "*.dll"));

            return coreDlls
                .Concat(runtimeDlls)
                .Concat(moduleDlls)
                .ToList();
        }

        private (ICollection<ComponentManifest>, ICollection<RouteManifest>) ExtractAssemblyComponents()
        {
            var resolver = new PathAssemblyResolver(ResolveAvailableDlls());
            using var context = new MetadataLoadContext(resolver);

            var assembly = context.LoadFromAssemblyName(AssemblyName);

            var componentTypes = assembly.GetTypes()
                .Where(t => t.BaseType.FullName == "Microsoft.AspNetCore.Components.ComponentBase");

            var components = componentTypes
                .Select(t => new ComponentManifest(t.FullName, t.FullName))
                .ToList();

            var routes = componentTypes.SelectMany(t => t.GetCustomAttributesData()
                .Where(a => a.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
                .Select(a => new RouteManifest(t.FullName, (string)a.ConstructorArguments[0].Value)))
                .ToList();

            return (components, routes);
        }

        private string GenerateJsonManifest(
            string assemblyName,
            IEnumerable<ComponentManifest> components,
            IEnumerable<RouteManifest> routes)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                {
                    assemblyName, new Dictionary<string, object>
                    {
                        {
                           "Components",
                            components
                        },
                        {
                            "Routes",
                            routes
                        },
                    }
                }
            });
        }

        private void Info(string message, params object[] args)
        {
            Log.LogMessage(MessageImportance.High, message, args);
        }
    }

    public sealed class ComponentManifest
    {
        public string TypeFullName { get; }

        public string ComponentName { get; }

        public ComponentManifest(
            string typeFullName,
            string componentName)
        {
            TypeFullName = typeFullName;
            ComponentName = componentName;
        }
    }

    public sealed class RouteManifest
    {
        public string TypeFullName { get; }

        public string Route { get; }

        public RouteManifest(
            string typeFullName,
            string route)
        {
            TypeFullName = typeFullName;
            Route = route;
        }
    }
}
