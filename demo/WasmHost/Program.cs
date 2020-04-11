using System.Threading.Tasks;
using BlazorLazyLoading.Wasm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WasmHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            ConfigureServices(builder.Services);

            WebAssemblyHost host = builder.Build();

            await host.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddBaseAddressHttpClient();
            services.AddLazyLoading();
        }
    }
}
