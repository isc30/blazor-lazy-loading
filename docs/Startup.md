There are some cases where you require to run some actions right after a lazy **Assembly** or **Module** is loaded. This section covers the **Startup** class and how to use it.

# Creating a Startup (optional)

In order to create a valid **Startup** for your Assembly, it just needs to include a *public class* called `Startup` with a *public* `Configure()` method:

```cs
public class Startup
{
    // (optional) DI constructor
    // public Startup(...) { }

    // (alternative) public void Configure()
    public Task Configure()
    {
        Console.WriteLine("Startup Called!");
        return Task.Delay(2000);
    }
}
```

As you might probably noticed, the `Startup` **constructor** accept parameters **injected** from the current `IServiceProvider`.

That's all! The next time the assembly gets loaded it will find, construct and `await` your `Startup.Configure()` implementation.
