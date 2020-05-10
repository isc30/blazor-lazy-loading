A lazy loadable module is just a **.NET assembly (project)** with few extra steps at build time.

In order to covert your csproj to a lazy loadable module, it will require:
- To use `Microsoft.NET.Sdk.Razor` as SDK *(RazorLib project)*.
- To target `netstandard2.1`

After checking that, just add:
```xml
<PackageReference Include="BlazorLazyLoading.Module" Version="1.2.0" PrivateAssets="all" />
```

and... **done!** This project is now a module and can be lazy loaded 😄

> ℹ️ `BlazorLazyLoading.Module` will **NOT** add any runtime dependency, it just contains build tooling.

# Building the Module

When building the module, you will notice how 2 *paths* are automatically generated.<br/>
Their content is very useful when debugging 😄

- **wwwroot/_lazy.json** *(manifest file)*

  This file includes metadata to be consumed by the client in order to effectively find resources inside the Module.<br/>As for v1 it includes **Razor Components and Pages** (with routing).


- **wwwroot/_lazy/** *(bin directory)*

  This folder will contain every runtime dependency (DLL) that this Module needs. It is required to support loading NuGet and project references effectively by Blazor.

These files will be automaticlly served as StaticWebAssets by the Blazor host.

# 🌟 Using "ModulesHost" (aggregated module)

Having *every* dependency copied to each module isn't a storage-friendly solution but it's perfect for creating completely isolated modules. The "aggregated" module approach solves the storage and discoverability issue by completely isolating the assemblies from BlazorLazyLoading, except for a single one (`ModulesHost`) that will aggregate and serve all the lazy DLLs.

This approach is **always recommended** if:

- You are **NOT** aiming for **absolute dependency isolation** and **just want lazy loading**.
- You are **migrating** an **already existing application** to use BlazorLazyLoading.

Steps:

1. Remove the `BlazorLazyLoading.Module` reference from all your Lazy Modules (if you already converted them).

1. Create a new Module project **without any content or dependencies** (*I personally recommend `ModulesHost` as for the name*) and add a reference to **BlazorLazyLoading.Module**:
    ```xml
    <PackageReference Include="BlazorLazyLoading.Module" Version="1.2.0" PrivateAssets="all" />
    ```

1. Reference all the RazorLib projects that you want to lazy load by using `ProjectReference` or `PackageReference`. For example:
    ```xml
    <ItemGroup>
        <ProjectReference Include="../MyComponents.csproj" />
        <PackageReference Include="isc30.MyPages" />
    </ItemGroup>
    ```

2. Include the referenced Assemblies in the Manifest generation:<br/>
    ```xml
    <ItemGroup>
        <BLLManifestAssemblies Include="MyComponents" />

        <!-- notice the assembly name for the nuget package -->
        <BLLManifestAssemblies Include="MyPages" />
    </ItemGroup>
    ```

# Advanced scenario: Skipping the Manifest Generation

If you wish to load the module manually by using `IAssemblyLoader`, you can disable the manifest generation step by setting `BLLGenerateManifest` to `false`:

```xml
<PropertyGroup>
    <BLLGenerateManifest>false<BLLGenerateManifest>
</PropertyGroup>
```