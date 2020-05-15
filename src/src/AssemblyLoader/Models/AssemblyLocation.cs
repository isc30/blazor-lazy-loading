namespace BlazorLazyLoading.Models
{
    /// <summary>
    /// Defines DLL and PDB paths for an assembly
    /// </summary>
    public sealed class AssemblyLocation
    {
        /// <summary>
        /// Path where the DLL might be located
        /// </summary>
        public string DllPath { get; }

        /// <summary>
        /// Path where the PDB might be located
        /// </summary>
        public string? PdbPath { get; }

        /// <inheritdoc/>
        public AssemblyLocation(
            string dllPath,
            string? pdbPath)
        {
            DllPath = dllPath;
            PdbPath = pdbPath;
        }
    }
}
