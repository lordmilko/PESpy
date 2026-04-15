namespace PESpy.Ecma335
{
    public sealed class AssemblyRefOSTable : Table<AssemblyRefOSRow>
    {
        internal readonly int OSPlatformIDOffset;
        internal readonly int OSMajorVersionOffset;
        internal readonly int OSMinorVersionOffset;
        internal readonly int AssemblyRefOffset;

        private readonly bool isBigAssemblyRefIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal AssemblyRefOSTable(
            int numRows,
            int assemblyRefIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigAssemblyRefIndex = assemblyRefIndexSize == 4;

            OSPlatformIDOffset = 0;
            OSMajorVersionOffset = OSPlatformIDOffset + sizeof(int);
            OSMinorVersionOffset = OSMajorVersionOffset + sizeof(int);
            AssemblyRefOffset = OSMinorVersionOffset + sizeof(int);
            RowSize = AssemblyRefOffset + assemblyRefIndexSize;
        }

        public int GetOSPlatformID(AssemblyRefOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSPlatformIDOffset);
        }

        public int GetOSMajorVersion(AssemblyRefOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSMajorVersionOffset);
        }

        public int GetOSMinorVersion(AssemblyRefOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSMinorVersionOffset);
        }

        public AssemblyRefIndex GetAssemblyRef(AssemblyRefOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (AssemblyRefIndex) tableChunk.PeekEcmaIndex(rowOffset + AssemblyRefOffset, isBigAssemblyRefIndex);
        }

        public int GetRowOffset(AssemblyRefOSIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefOSRow this[AssemblyRefOSIndex index] => GetRowSafe((int) index);

        protected override AssemblyRefOSRow GetRow(int index) => new AssemblyRefOSRow((AssemblyRefOSIndex) index, this);
    }
}
