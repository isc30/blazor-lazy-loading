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
    public static class AssemblyLoaderStartupExtensions
    {
        public static IServiceCollection AddLazyLoading(
            this IServiceCollection services)
        {
            services.AddScoped<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton<IAssemblyLoadContextFactory, DisposableAssemblyLoadContextFactory>();

            services.AddSingleton(
                typeof(IAssemblyDataProvider),
                p =>
                {
                    IWebHostEnvironment env = p.GetRequiredService<IWebHostEnvironment>();
                    return new FileProviderAssemblyDataProvider(env.WebRootFileProvider);
                });

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
}
