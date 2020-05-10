using System;
using System.Threading.Tasks;

public class Startup
{
    public Task Configure()
    {
        Console.WriteLine("Startup!");
        return Task.Delay(2000);
    }
}
