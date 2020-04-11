using System.Collections.Generic;
using System.Reflection;

namespace BlazorLazyLoading.Models
{
    public sealed class AssemblyLoaderContext
    {
        public readonly AssemblyName AssemblyName;

        public readonly AssemblyLoaderContext? Parent = null;
        public readonly List<AssemblyLoaderContext> Children = new List<AssemblyLoaderContext>();

        public AssemblyLoaderContext(AssemblyName name)
        {
            AssemblyName = name;
        }

        private AssemblyLoaderContext(AssemblyName name, AssemblyLoaderContext parent)
        {
            Parent = parent;
            AssemblyName = name;
        }

        public AssemblyLoaderContext NewScope(AssemblyName name)
        {
            var scope = new AssemblyLoaderContext(name, this);
            Children.Add(scope);

            return scope;
        }
    }
}
