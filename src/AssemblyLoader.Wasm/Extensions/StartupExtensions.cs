using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Services;
using BlazorLazyLoading.Wasm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorLazyLoading.Wasm
{
    public static class AssemblyLoaderStartupExtensions
    {
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services)
        {
            services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton<IAssemblyLoadContextFactory, AppDomainAssemblyLoadContextFactory>();
            services.AddSingleton<IAssemblyDataProvider, NetworkAssemblyDataProvider>();

            return services;
        }
    }
}
