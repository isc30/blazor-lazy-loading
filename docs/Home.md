Welcome to *Blazor Lazy Loading* wiki!

Please use the **Wiki Index** panel to navigate to the different pages --->

***

# FAQ

- Is WebAssembly supported?

  >Yes, every host of Blazor is supported. This includes Blazor Server, WebAssembly DevServer, WebAssembly static (published), Prerendering and even DualMode.

- For WASM, can I keep the linker enabled?

  >Yes, even if disabling it doesn't make a big difference when your application uses lazy loading, you can provide a custom LinkerConfig.xml file to enable it. [Mode info...](https://github.com/isc30/blazor-lazy-loading/wiki/Configuring-Lazy-Loading-@-WebAssembly#configuring-the-linker)

- Can my Lazy Modules contain references to NuGet packages?

  >Yes, every nested dependency of a Module (including NuGet DLLs) will have a Lazy download that will be triggered only when required.