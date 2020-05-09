using System;
using System.Reflection;

namespace BlazorLazyLoading.Comparers
{
    public sealed class AssemblyByNameComparer : IAssemblyComparer
    {
        public static readonly AssemblyByNameComparer Default = new AssemblyByNameComparer();

        public bool Equals(Assembly x, Assembly y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        public bool Equals(AssemblyName x, AssemblyName y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        public int GetHashCode(Assembly obj)
        {
            return GetHashCode(obj.GetName());
        }

        public int GetHashCode(AssemblyName obj)
        {
            return obj.Name
                .ToLowerInvariant()
                .GetHashCode();
        }
    }
}
