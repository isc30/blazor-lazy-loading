namespace BlazorLazyLoading.Models
{
    public sealed class AssemblyData
    {
        public readonly byte[] DllBytes;
        public readonly byte[]? PdbBytes;

        public AssemblyData(
            byte[] dllBytes,
            byte[]? pdbBytes)
        {
            DllBytes = dllBytes;
            PdbBytes = pdbBytes;
        }
    }
}
