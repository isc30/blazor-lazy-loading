using BlazorLazyLoading.Abstractions;

namespace BlazorLazyLoading.Server.Services
{
    public sealed class DisposableAssemblyLoadContextFactory : IAssemblyLoadContextFactory
    {
        public IAssemblyLoadContext Create(string name)
        {
            return new DisposableAssemblyLoadContext(name);
        }
    }
}
