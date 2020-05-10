using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorLazyLoading.Services
{
    public static class AssemblyInitializer
    {
        public static Task ConfigureAssembly(Assembly assembly, IServiceProvider services)
        {
            var startupTypes = assembly.GetTypes()
                .Where(t => t.Name == "Startup")
                .Where(t => t.GetMethod("Configure", Array.Empty<Type>()) != null)
                .ToList();

            if (startupTypes.Count != 1)
            {
                return Task.CompletedTask;
            }

            var startupType = startupTypes.First();
            var configureMethod = startupType.GetMethod("Configure", Array.Empty<Type>());

            var startup = ActivatorUtilities.CreateInstance(services, startupType);
            object result = configureMethod.Invoke(startup, Array.Empty<object>());

            if (result is Task resultTask)
            {
                return resultTask;
            }

            return Task.CompletedTask;
        }
    }
}
