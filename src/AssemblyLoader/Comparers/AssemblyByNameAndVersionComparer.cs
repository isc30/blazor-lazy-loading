using System;
using System.Reflection;

namespace BlazorLazyLoading.Comparers
{
    public sealed class AssemblyByNameAndVersionComparer : IAssemblyComparer
    {
        public static readonly AssemblyByNameAndVersionComparer Default = new AssemblyByNameAndVersionComparer();

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
            return HashCode.Combine(
                obj.Name.ToLowerInvariant(),
                obj.Version?.ToString() ?? "0.0.0.0");
        }
    }
}
