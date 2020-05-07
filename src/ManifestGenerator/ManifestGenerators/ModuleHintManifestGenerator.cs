using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorLazyLoading.ManifestGenerators
{
    public sealed class ModuleHintManifestGenerator : IManifestGenerator
    {
        private readonly Logger _logger;

        public ModuleHintManifestGenerator(Logger logger)
        {
            _logger = logger;
        }

        public Dictionary<string, object>? GenerateManifest(Assembly assembly, MetadataLoadContext metadataLoadContext)
        {
            // If the assembly has dependency with a reference to BlazorLazyLoading.Module, automate the ModuleHint
            var moduleHints = new List<string>();

            bool isLazyModule = assembly.GetCustomAttributesData().Any(d => d.AttributeType.Name == "BlazorLazyLoadingModuleAttribute");

            if (isLazyModule)
            {
                moduleHints.Add(assembly.GetName().Name);
            }

            if (!moduleHints.Any())
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                { "ModuleHints", moduleHints }
            };
        }
    }
}
