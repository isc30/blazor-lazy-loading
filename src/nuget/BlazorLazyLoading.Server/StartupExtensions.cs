using System;
using System.Collections.Generic;
using System.Net.Mime;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Server.Services;
using BlazorLazyLoading.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorLazyLoading.Server
{
    /// <summary>
    /// Server startup extensions for BlazorLazyLoading
    /// </summary>
    public static class BLLServerStartupExtensions
    {
        /// <summary>
        /// Registers BlazorLazyLoading services
        /// </summary>
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services,
            LazyLoadingOptions options)
        {
            if (options.UseAssemblyIsolation)
            {
                services.AddScoped<IAssemblyLoader>(CreateAssemblyLoader);
                services.AddScoped<IAssemblyLoadContext, DisposableAssemblyLoadContext>();
            }
            else
            {
                services.AddSingleton<IAssemblyLoader>(CreateAssemblyLoader);
                services.AddSingleton<IAssemblyLoadContext, DisposableAssemblyLoadContext>();
            }

            services.AddSingleton<IAssemblyDataProvider, AssemblyDataProvider>();
            services.AddSingleton(typeof(IAssemblyDataLocator), options.AssemblyDataLocator ?? typeof(AssemblyDataLocator));

            services.AddSingleton<IContentFileReader>(
                p =>
                {
                    IWebHostEnvironment env = p.GetRequiredService<IWebHostEnvironment>();
                    return new FileProviderContentFileReader(env.WebRootFileProvider);
                });

            services.AddSingleton<ILazyModuleHintsProvider>(
                p => new LazyModuleHintsProvider(options.ModuleHints));

            services.AddSingleton(typeof(IManifestLocator), options.ManifestLocator ?? typeof(ManifestLocator));
            services.AddSingleton<IManifestRepository, ManifestRepository>();

            return services;
        }

        /// <summary>
        /// Configures the host to use BlazorLazyLoading
        /// </summary>
        public static void UseLazyLoading(
            this IApplicationBuilder app)
        {
            // support blazor _content for .dll and .pdb
            var contentTypeMap = new FileExtensionContentTypeProvider();
            contentTypeMap.Mappings[".dll"] = MediaTypeNames.Application.Octet;
            contentTypeMap.Mappings[".pdb"] = MediaTypeNames.Application.Octet;

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeMap,
            });
        }

        private static IAssemblyLoader CreateAssemblyLoader(IServiceProvider p)
        {
            var assemblyLoader = ActivatorUtilities.CreateInstance<AssemblyLoader>(p);
            assemblyLoader.SubscribeOnAssemblyLoad(a => AssemblyInitializer.ConfigureAssembly(a, p));

            return assemblyLoader;
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
        /// <br>Configures assembly isolation level. Do NOT set this to 'false' unless you want to share 'static' fields between users.</br>
        /// <br>Keeping this enabled ensures that the server can be scaled horizontally.</br>
        /// <br>default: true</br>
        /// </summary>
        public bool UseAssemblyIsolation { get; set; } = true;

        /// <summary>
        /// Configures a custom IAssemblyDataLocator. The type must implement BlazorLazyLoading.Abstractions.IAssemblyDataLocator
        /// </summary>
        public Type? AssemblyDataLocator { get; set; } = null;

        /// <summary>
        /// Configures a custom IManifestLocator. The type must implement BlazorLazyLoading.Abstractions.IManifestLocator
        /// </summary>
        public Type? ManifestLocator { get; set; } = null;
    }
}
