In this section, we will cover each of the tools in the package separately so you can customize everything.

# Manually Loading DLLs
It is possible to trigger DLL loads manually by injecting `IAssemblyLoader` and calling `LoadAssemblyByNameAsync`. This will automatically solve all the concurrency problems for you and fetch the DLLs if required using `IAssemblyDataProvider` internally.

# Manually Handling Assembly Downloads
In order to change how the DLL data gets retrieved, you can override the implementation of `IAssemblyDataProvider`. This will give you full control on how to fetch for assemblies. It uses `IAssemblyDataLocator` underneath in case you just want to customize the paths from where the assemblies get downloaded.
