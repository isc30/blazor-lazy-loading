using System.Collections.Generic;
using System.Reflection;

namespace BlazorLazyLoading
{
    public interface IManifestGenerator
    {
        public Dictionary<string, object>? GenerateManifest(Assembly assembly);
    }
}
