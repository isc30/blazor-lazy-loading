using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BlazorLazyLoading.ManifestGenerators;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace BlazorLazyLoading
{
    public class GenerateManifest : Task
    {
        private readonly ICollection<IManifestGenerator> _manifestGenerators;

        private Logger _logger;

        [Required]
        public string[] AssemblyNames { get; set; } = Array.Empty<string>();

        [Required]
        public string[] AssemblyPaths { get; set; } = Array.Empty<string>();

        [Required]
        public string ManifestOutputPath { get; set; } = string.Empty;

        public GenerateManifest()
        {
            _logger = new Logger(Log);

            _manifestGenerators = new IManifestGenerator[]
            {
                new ComponentManifestGenerator(_logger),
                new RouteManifestGenerator(_logger),
            };
        }

        public override bool Execute()
        {
            SanitizeInput();

            var manifest = new Dictionary<string, IDictionary>();

            var dlls = ResolveAvailableDlls().ToList();
            using var metadataLoadContext = CreateDllMetadataLoadContext(dlls);

            foreach (string assemblyName in AssemblyNames)
            {
                try
                {
                    _logger.Debug($"Generating Manifest file for '{assemblyName}':");
                    Assembly assembly = metadataLoadContext.LoadFromAssemblyName(assemblyName);
                    _logger.Debug($"Assembly loaded: {assemblyName}");

                    Dictionary<string, object>? manifestSections = ExecuteManifestGenerators(assembly, metadataLoadContext);

                    if (manifestSections == null)
                    {
                        _logger.Debug($"Lazy Module '{assemblyName}' has no relevant manifest sections");
                        manifestSections = new Dictionary<string, object>();
                    }

                    manifest.Add(assemblyName, manifestSections);

                    if (manifestSections.Any())
                    {
                        var manifestDescriptions = manifestSections.Select(
                            s => "'" + s.Key + "'" + (s.Value is ICollection c ? ": " + c.Count : string.Empty) + "");

                        _logger.Info($"Manifest for '{assemblyName}' generated: {{ {string.Join(", ", manifestDescriptions)} }}");
                    }
                    else
                    {
                        _logger.Info($"Manifest for '{assemblyName}' generated with no content");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    return false;
                }
            }

            string manifestJson = JsonConvert.SerializeObject(manifest);
            File.WriteAllText(ManifestOutputPath, manifestJson);

            return true;
        }

        private void SanitizeInput()
        {
            AssemblyPaths = AssemblyPaths.Select(p => p
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
                .Distinct()
                .ToArray();

            AssemblyNames = AssemblyNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToArray();
        }

        private IEnumerable<string> ResolveAvailableDlls()
        {
            var coreDlls = new[] { typeof(object).Assembly.Location };
            var runtimeDlls = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var moduleDlls = AssemblyPaths.SelectMany(p => Directory.GetFiles(p, "*.dll"));

            return coreDlls
                .Concat(runtimeDlls)
                .Concat(moduleDlls)
                .Distinct()
                .ToList();
        }

        private MetadataLoadContext CreateDllMetadataLoadContext(IEnumerable<string> dlls)
        {
            var resolver = new PathAssemblyResolver(dlls);
            return new MetadataLoadContext(resolver);
        }

        private Dictionary<string, object>? ExecuteManifestGenerators(Assembly assembly, MetadataLoadContext metadataLoadContext)
        {
            var manifestSections = new Dictionary<string, object>();

            foreach (var manifestGenerator in _manifestGenerators)
            {
                var manifestSection = manifestGenerator.GenerateManifest(assembly, metadataLoadContext);

                if (manifestSection == null)
                {
                    continue;
                }

                foreach (var keyValue in manifestSection)
                {
                    if (manifestSections.ContainsKey(keyValue.Key))
                    {
                        throw new NotSupportedException("Duplicated manifest section keys");
                    }

                    manifestSections.Add(keyValue.Key, keyValue.Value);
                }
            }

            return manifestSections.Any()
                ? manifestSections
                : null;
        }
    }
}
