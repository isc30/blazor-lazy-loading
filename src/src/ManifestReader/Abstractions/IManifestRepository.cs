using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorLazyLoading.Models;

namespace BlazorLazyLoading.Abstractions
{
    public interface IManifestRepository
    {
        public Task<ModuleManifest?> GetByModuleNameAsync(string moduleName);

        public Task<ICollection<ModuleManifest>> GetAllAsync();
    }
}
