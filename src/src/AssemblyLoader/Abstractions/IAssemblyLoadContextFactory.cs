namespace BlazorLazyLoading.Abstractions
{
    public interface IAssemblyLoadContextFactory
    {
        IAssemblyLoadContext Create(string name);
    }
}
