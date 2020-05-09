# &lt;Lazy /&gt;

Renders a Component (`IComponent`) from a Lazy Module based on it's **LazyName** or **TypeFullName** (*string*).

This Component needs to exist in a *known* Lazy Module, either [hinted directly](Configuring-Lazy-Loading-@-Server#modulehints) or using [ModulesHost strategy](Creating-a-Lazy-Loadable-Module#creating-an-aggregated-module).

# Render by using TypeFullName

The is no extra prerequisite to render an `IComponent` by using **TypeFullName**:

```razor
<Lazy Name="MyApplication.Hello" />
```

This snippet renders a `Hello` Component assuming the type is `MyApplication.Hello`. It will **automatically fetch** the manifests, consume them to **locate** the Component and **download** the minimum assemblies to make it work 😄

# Named Components

It is possible to **manually name** your Components and use that name later to resolve them.<br />
This is done by adding a simple **attribute** `BlazorLazyLoading.LazyNameAttribute` to your Component:

* Syntax: `.razor`
    ```razor
    @attribute [LazyName("SayHello")]
    <h1>Hello!</h3>
    ```

* Syntax: `.cs`
    ```cs
    [LazyName("SayHello")]
    public class Hello : IComponent { ... }
    ```

And then **render** it like the following:

```razor
<Lazy Name="SayHello" />
```

In order to *debug* if your component **name** is generated properly, you can check the contents of `_lazy.json`.
```json
{
    "MyApplication": {
        "Components": [
            {
                "TypeFullName": "MyApplication.Hello",
                "Name": "SayHello"
            }
        ]
    }
}
```

# Custom 'Loading' view

It is possible to customize the `<Lazy>` Component with a **loading** `RenderFragment` that will get rendered while everything is being initialized.

It only requires setting a `RenderFragment` called `Loading`:

```razor
<Lazy Name="SayHello">
    <Loading>
        <p>Loading, please wait</p>
        <div class="spinner" />
    </Loading>
</Lazy>
```

# Custom 'Error' view

It is possible to customize the 'error' `RenderFragment` that gets renderd if the Component fails to initialize *(because some assembly failed to load, such element doesn't exist, etc)*.

It only requires setting a `RenderFragment` called `Error`:

```razor
<Lazy Name="SayHello">
    <Error>
        <p class="error">Something bad happened</p>
    </Error>
</Lazy>
```

# Handling Errors

It is possible to specify if a Component is **Required** (if a loading error throws an exception) by setting the property `Required` *(bool)* on it.

```razor
<Lazy Required="false" Name="Unknown" /> @* this will simply not get rendered *@
<Lazy Required="true" Name="Unknown" /> @* this will throw an exception *@
@* both will render the 'error' view *@
```

# Hooking into Events

There are 2 Events you can hook to by just setting Callbacks in the Component:

* #### OnBeforeLoadAsync

    This callback will be awaited **before** trying to resolve the Component from the manifests.
    Useful for delaying a Component render to perform another operation before, and Debugging with `Task.Delay`.

* #### OnAfterLoad

    This callback will be invoked **after** resolving and rendering the lazy Component.

```razor
<Lazy
    Name="SayHello"
    OnBeforeLoadAsync="l => Task.Delay(500)"
    OnAfterLoad='l => Console.WriteLine($"Loaded LazyComponent: {l.Name}")' />
```

# SEO and Prerendering Support

Don't worry about SEO. If you use BlazorServer or Prerendering, the Component HTML will be **fully rendered** automatically by the server so there is no difference between rendering it directly and using `<Lazy>`.