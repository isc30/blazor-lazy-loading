using System;
using System.Collections.Generic;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Services;
using BlazorLazyLoading.Wasm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorLazyLoading.Wasm
{
    /// <summary>
    /// WebAssembly startup extensions for BlazorLazyLoading
    /// </summary>
    public static class BLLWasmStartupExtensions
    {
        /// <summary>
        /// Registers BlazorLazyLoading services
        /// </summary>
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services,
            LazyLoadingOptions options)
        {
            services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton<IAssemblyLoadContextFactory, AppDomainAssemblyLoadContextFactory>();
            services.AddSingleton(typeof(IAssemblyDataLocator), options.AssemblyDataLocator ?? typeof(AssemblyDataLocator));
            services.AddSingleton<IContentFileReader, NetworkContentFileReader>();
            services.AddSingleton<IAssemblyDataProvider, AssemblyDataProvider>();

            services.AddSingleton<IManifestLocator, ManifestLocator>();
            services.AddSingleton<IManifestRepository, ManifestRepository>();

            services.AddSingleton<ILazyModuleHintsProvider>(
                p => new LazyModuleHintsProvider(options.ModuleHints));

            return services;
        }
    }

    /// <summary>
    /// BlazorLazyLoading options
    /// </summary>
    public sealed class LazyLoadingOptions
    {
        /// <summary>
        /// Specifies a list of Module Names (hints) to download DLLs from them and use their manifest to locate lazy resources
        /// </summary>
        public IEnumerable<string> ModuleHints { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Configures a custom AssemblyDataLocator. The type must implement IAssemblyDataLocator.
        /// </summary>
        public Type? AssemblyDataLocator { get; set; } = null;
    }
}
