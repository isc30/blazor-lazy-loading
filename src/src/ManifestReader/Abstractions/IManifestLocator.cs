using System.Collections.Generic;

namespace BlazorLazyLoading.Abstractions
{
    /// <summary>
    /// Locates _lazy.json manifests
    /// </summary>
    public interface IManifestLocator
    {
        /// <summary>
        /// Returns a list of possible paths that contain lazy manifests
        /// </summary>
        public IEnumerable<string> GetManifestPaths();
    }
}
