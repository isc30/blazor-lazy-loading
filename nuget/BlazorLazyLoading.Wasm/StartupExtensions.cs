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
            services.AddSingleton<IAssemblyDataLocator, AssemblyDataLocator>();
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
        /// <br>Specifies a list of Module Names (hints) to:</br>
        /// <br>  - Download DLLs from them</br>
        /// <br>  - Use their manifest to locate lazy resources</br>
        /// </summary>
        public IEnumerable<string> ModuleHints { get; set; } = Array.Empty<string>();
    }
}
