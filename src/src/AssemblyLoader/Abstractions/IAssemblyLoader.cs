using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyLoader
    {
        Assembly? GetLoadedAssemblyByName(AssemblyName assemblyName);

        Task<Assembly?> LoadAssemblyByNameAsync(AssemblyName assemblyName);

        void SubscribeOnAssemblyLoad(Func<Assembly, Task> callback);

        void UnsubscribeOnAssemblyLoad(Func<Assembly, Task> callback);
    }
}
