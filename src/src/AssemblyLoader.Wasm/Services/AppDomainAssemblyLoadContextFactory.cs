using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Wasm.Services
{
    public sealed class AppDomainAssemblyLoadContextFactory : IAssemblyLoadContextFactory
    {
        public IAssemblyLoadContext Create(string name)
        {
            return new AppDomainAssemblyLoadContext(name);
        }
    }
}
