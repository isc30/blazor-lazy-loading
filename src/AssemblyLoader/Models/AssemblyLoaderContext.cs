using System.Collections.Generic;
using System.Reflection;

namespace BlazorLazyLoading.Models
{
    /// <summary>
    /// Describes the tree of loaded assembly dependencies
    /// </summary>
    public sealed class AssemblyLoaderContext
    {
        /// <summary>
        /// Assembly Name
        /// </summary>
        public readonly AssemblyName AssemblyName;

        /// Parent Node
        public readonly AssemblyLoaderContext? Parent = null;

        /// Children Nodes
        public readonly List<AssemblyLoaderContext> Children = new List<AssemblyLoaderContext>();

        /// Constructs the AssemblyLoaderContext
        public AssemblyLoaderContext(AssemblyName name)
        {
            AssemblyName = name;
        }

        private AssemblyLoaderContext(AssemblyName name, AssemblyLoaderContext parent)
        {
            Parent = parent;
            AssemblyName = name;
        }

        /// <summary>
        /// Creates a new scope for the current AssemblyLoaderContext
        /// </summary>
        public AssemblyLoaderContext NewScope(AssemblyName name)
        {
            var scope = new AssemblyLoaderContext(name, this);
            Children.Add(scope);

            return scope;
        }
    }
}
