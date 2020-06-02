The following nuget package includes everything you need to support lazy loading on **Blazor WebAssembly** (when statically published, using DevServer or using [DualMode](https://github.com/Suchiman/BlazorDualMode)).<br/>

It can be installed by adding the following line inside the host csproj:

```xml
<PackageReference Include="BlazorLazyLoading.Wasm" Version="1.3.0" PrivateAssets="all" />
```

It will also require to be initialized from **Program.cs** by adding the following lines:<br/>

```cs
builder.Services.AddLazyLoading(new LazyLoadingOptions
{
    ModuleHints = new[] { "ModulesHost" }
});
```

# Configuring the Linker

In order to load assemblies dynamically, the linker can be a big issue since it is **enabled** for release builds **by default**.

There are 2 ways of approaching this:

### Disabling the Linker (easy)

This option sounds worse that it is in reality. Yes, disabling the linker will ship mscorlib as initial download to the browser, **BUT that's all**. The initial impact won't be big if we Lazy Load the rest of the application.

```xml
<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>false</BlazorWebAssemblyEnableLinking>
</PropertyGroup>
```

### Using LinkerConfig.xml (advanced)

Even if this solution is more advanced, it will give us the best performance. Please refer to [the following guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/blazor/configure-linker?view=aspnetcore-3.1#control-linking-with-a-configuration-file) to setup your *LinkerConfig.xml*.

The recommendation here would be something like this, but you might need to adapt it for your needs:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<linker>
    <assembly fullname="netstandard" /> <!-- keep full netstandard since its used by the lazy modules -->
    <assembly fullname="ModulesHost" /> <!-- keep full entrypoint -->
</linker>
```

Done! Lazy Loading is now configured 😄

> Due to a bug in Blazor's linker, the user assemblies will be downloaded as part of the boot but not in memory. The library handles this case gracefully so it shouldn't be a problem until the linker gets fixed. [More info...](https://github.com/isc30/blazor-lazy-loading/issues/60)

# LazyLoadingOptions

* #### ModuleHints

  In order to find the `_lazy.json` manifest files and DLLs, you need to specify *at least* an entry-point to a lazy module. This **must** be done by passing the *"known modules"* as **string**.

  >Specifies a list of Module Names (hints) to:
  >- Download DLLs from them
  >- Use their manifest to locate lazy resources