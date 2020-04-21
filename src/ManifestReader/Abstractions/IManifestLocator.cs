using System.Collections.Generic;

namespace BlazorLazyLoading.Abstractions
{
    public interface IManifestLocator
    {
        /// <summary>
        /// Returns a list of possible paths that contain lazy manifests
        /// </summary>
        public IEnumerable<string> GetManifestPaths();
    }
}
