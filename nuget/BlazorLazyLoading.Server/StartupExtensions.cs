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
    public static class BLLServerStartupExtensions
    {
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services,
            LazyLoadingOptions options)
        {
            services.AddScoped<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton<IAssemblyLoadContextFactory, DisposableAssemblyLoadContextFactory>();
            services.AddSingleton<IAssemblyDataLocator, AssemblyDataLocator>();
            services.AddSingleton<IAssemblyDataProvider, AssemblyDataProvider>();

            services.AddSingleton<IContentFileReader>(
                p =>
                {
                    IWebHostEnvironment env = p.GetRequiredService<IWebHostEnvironment>();
                    return new FileProviderContentFileReader(env.WebRootFileProvider);
                });

            services.AddSingleton<ILazyModuleHintsProvider>(
                p => new LazyModuleHintsProvider(options.ModuleHints));

            services.AddSingleton<IManifestLocator, ManifestLocator>();
            services.AddSingleton<IManifestRepository, ManifestRepository>();

            return services;
        }

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
