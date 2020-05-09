# &lt;LazyRouter /&gt;

Provides SPA navigation for Pages and Routes in Lazy Modules. It is a direct replacement of `<Router>`.

The pages need to exist in a *known* Lazy Module, either [hinted directly](Configuring-Lazy-Loading-@-Server#modulehints) or using [ModulesHost strategy](Creating-a-Lazy-Loadable-Module#creating-an-aggregated-module).

# Creating Lazy Pages and Routes

Pages and Routes don't require any configuration, just create them as you would normally (using `@page` attribute, etc).

If the Page is contained in a **Module**, the manifest *(_lazy.json)* will already generate all the information for routing automatically:

```json
{
    "MyApplication": {
        "Routes": [
            {
                "Route": "/counter",
                "Name": "CounterPage"
            }
        ]
    }
}
```

# Using LazyRouter

As mentioned before, this is a **direct replacement** of `Router` so it's safe to replace one with another in your `App.razor` file.

```diff
-  <Router AppAssembly="@typeof(Program).Assembly">
+  <LazyRouter AppAssembly="@typeof(Program).Assembly">
      <Found Context="routeData">
          <LayoutView Layout="@typeof(MainLayout)">
              <RouteView RouteData="@routeData" />
          </LayoutView>
      </Found>
      <NotFound>
          <LayoutView Layout="@typeof(MainLayout)">
              <p>Sorry, there's nothing at this address</p>
          </LayoutView>
      </NotFound>
-  </Router>
+  </LazyRouter>
```

Done! The router will automatically provide SPA style navigation for your lazy routes!

# Custom 'Loading' screen

It is possible to customize the 'loading' screen while a Page is downloading the needed Assemblies.

This is done by setting the `Loading` `RenderFragment`:

```razor
<LazyRouter AppAssembly="@typeof(Program).Assembly">
    <Loading Context="moduleName">
        <LayoutView Layout="@typeof(MainLayout)">
            <div class="fullScreenOverlay">
                <LoadingSpinner />
            </div>
        </LayoutView>
    </Loading>
    @* ... the rest of <Found> and <NotFound> *@
</LazyRouter>
```

# SEO and Prerendering Support

There is no need to worry about SEO. If you use BlazorServer or Prerendering, the returned page HTML will be **fully rendered** automatically by the server so there is no difference between the prerendered HTML content of `<Router>` and `<LazyRouter>`.