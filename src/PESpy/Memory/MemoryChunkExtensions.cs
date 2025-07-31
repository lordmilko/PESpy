using PESpy.PDB;

namespace PESpy
{
    internal static class MemoryChunkExtensions
    {
        public static PEFile PEFile(this MemoryChunk chunk) => (PEFile) (chunk.block.Provider as IFileMemoryBlockProvider)?.File!;
        public static PDBFile PDBFile(this MemoryChunk chunk) => ((PagedMemoryBlock) chunk.block).PDBFile;
        public static PortablePDBFile PortablePDBFile(this MemoryChunk chunk) => (PortablePDBFile) ((GlobalMemoryBlock) chunk.block).File;

        public static IFile File(this MemoryChunk chunk)
        {
            var block = chunk.block;

            if (block.Provider is IFileMemoryBlockProvider f)
                return f.File;

            if (block is GlobalMemoryBlock g)
                return g.File;

            if (block is PagedMemoryBlock p)
                return p.PDBFile;

            throw new System.NotImplementedException();
        }
    }
}
