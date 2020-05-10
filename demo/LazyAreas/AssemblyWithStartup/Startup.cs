using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace AssemblyWithStartup
{
    public class Startup
    {
        private readonly IJSRuntime _jsRuntime;

        public Startup(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public Task Configure()
        {
            return Alert("Hello from Startup! The assembly 'AssemblyWithStartup' has been loaded ;)");
        }

        private async Task Alert(string message)
        {
            // avoid crash on prerendering
            try
            {
                await _jsRuntime.InvokeVoidAsync("alert", message).ConfigureAwait(false);
            }
            catch (Exception)
            { }
        }
    }
}
