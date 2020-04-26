using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorLazyLoading.Wasm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModulesHost.Components;

namespace WasmHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            ConfigureServices(builder.Services, builder);

            WebAssemblyHost host = builder.Build();

            await host.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            AddHttpClient(services, builder);

            Type x = typeof(SimplePage);

            services.AddLazyLoading(new LazyLoadingOptions
            {
                ModuleHints = new[] { "ModulesHost" }
            });
        }

        private static void AddHttpClient(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            services.AddSingleton(
                typeof(HttpClient),
                p => new HttpClient
                {
                    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
                });
        }
    }
}
