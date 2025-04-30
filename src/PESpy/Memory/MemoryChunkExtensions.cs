using PESpy.PDB;

namespace PESpy
{
    internal static class MemoryChunkExtensions
    {
        public static PEFile PEFile(this MemoryChunk chunk) => (PEFile) ((IFileMemoryBlockProvider) chunk.block.Provider!).File;
        public static PDBFile PDBFile(this MemoryChunk chunk) => ((PagedMemoryBlock) chunk.block).PDBFile;
    }
}
