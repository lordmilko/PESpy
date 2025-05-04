namespace PESpy.Ecma335
{
    public sealed class AssemblyRefOSTable : Table<AssemblyRefOSRow>
    {
        internal readonly int RowSize;

        private readonly int OSPlatformIDOffset;
        private readonly int OSMajorVersionOffset;
        private readonly int OSMinorVersionOffset;
        private readonly int AssemblyRefOffset;

        private readonly bool isBigAssemblyRefIndex;

        private readonly MemoryChunk tableChunk;

        internal AssemblyRefOSTable(int numRows, int assemblyRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetAssemblyRef(AssemblyRefOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + AssemblyRefOffset, isBigAssemblyRefIndex);
        }

        public int GetRowOffset(AssemblyRefOSIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefOSRow this[AssemblyRefOSIndex index] => this[(int) index];

        protected override AssemblyRefOSRow GetRow(int index) => new AssemblyRefOSRow((AssemblyRefOSIndex) index, this);
    }
}
