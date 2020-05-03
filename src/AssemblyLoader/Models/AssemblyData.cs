namespace BlazorLazyLoading.Models
{
    /// <summary>
    /// Contains the data required to load an assembly
    /// </summary>
    public sealed class AssemblyData
    {
        /// <summary>Bytes from the DLL</summary>
        public readonly byte[] DllBytes;

        /// <summary>Bytes from the PDB</summary>
        public readonly byte[]? PdbBytes;

        /// Constructs AssemblyData
        public AssemblyData(
            byte[] dllBytes,
            byte[]? pdbBytes)
        {
            DllBytes = dllBytes;
            PdbBytes = pdbBytes;
        }
    }
}
