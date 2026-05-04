using System.Diagnostics;

namespace PESpy
{
    //CORCOMPILE_IMPORT_TABLE_ENTRY
    [Source(SourceKind.corcompile_h)]
    [DebuggerDisplay("wAssemblyRid = {wAssemblyRid}, wModuleRid = {wModuleRid}")]
    public readonly struct CorCompileImportTableEntry : IValue
    {
        private const int wAssemblyRidOffset = 0;
        private const int wModuleRidOffset = 2;

        public ushort wAssemblyRid => chunk.PeekUInt16(wAssemblyRidOffset);

        public ushort wModuleRid => chunk.PeekUInt16(wModuleRidOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //wAssemblyRid
            sizeof(ushort); //wModuleRid

        private readonly MemoryChunk chunk;

        internal CorCompileImportTableEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
