The following nuget package includes everything you need to support lazy loading on **Blazor Server** and **Prerendering**.<br/>

It can be installed by adding the following line inside the host csproj:

```xml
<PackageReference Include="BlazorLazyLoading.Server" Version="1.1.0" PrivateAssets="all" />
```

It will also require to be initialized from **Startup.cs** by adding the following lines:<br/>

```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLazyLoading(new LazyLoadingOptions
        {
            ModuleHints = new[] { "ModulesHost" }
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // ideally after calling app.UseStaticFiles()
        app.UseLazyLoading(); // serves DLL and PDB files as octet/stream
    }
}
```
# LazyLoadingOptions

* #### ModuleHints

  In order to find the `_lazy.json` manifest files and DLLs, you need to specify *at least* an entry-point to a lazy module. This **must** be done by passing the *"known modules"* as **string**.

  >Specifies a list of Module Names (hints) to:
  >- Download DLLs from them
  >- Use their manifest to locate lazy resources

* #### UseAssemblyIsolation

  Serverside Blazor has a small disadvantage: by default, the loaded assemblies are in **the same context** for every user. If you have a `static` field in them, the value will be shared accross all SignalR connections. If this happens, it can introduce weird bugs and massive scalability issues (*it could also happen with a nuget package using a static field internally*).

  To avoid these issues, BlazorLazyLoading introduces full assembly isolation by creating a Scoped `AssemblyLoadContext`. Unless you really know what you are doing, it is recommended to **NOT** turn this off.

  >default: true
  >Configures assembly isolation level. Do NOT set this to 'false' unless you want to share 'static' fields between users.<br />
  >Keeping this enabled ensures that the server can be scaled horizontally.<br />
