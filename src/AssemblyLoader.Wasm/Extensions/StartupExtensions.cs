﻿using System;
using System.Collections.Generic;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Services;
using BlazorLazyLoading.Wasm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorLazyLoading.Wasm
{
    public static class AssemblyLoaderStartupExtensions
    {
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services,
            LazyLoadingOptions options)
        {
            services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton<IAssemblyLoadContextFactory, AppDomainAssemblyLoadContextFactory>();
            services.AddSingleton<IAssemblyDataProvider, NetworkAssemblyDataProvider>();
            services.AddSingleton<IAssemblyDataLocator, AssemblyDataLocator>();

            services.AddSingleton<ILazyModuleHintsProvider>(
                p => new LazyModuleHintsProvider(options.ModuleHints));

            return services;
        }
    }

    public sealed class LazyLoadingOptions
    {
        /// <summary>
        /// Specifies a list of Module Names (hints) to:
        ///   - Download DLLs from them
        ///   - Use their manifest to locate lazy resources
        /// </summary>
        public IEnumerable<string> ModuleHints { get; set; } = Array.Empty<string>();
    }
}
