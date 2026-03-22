namespace PESpy.Ecma335
{
    public sealed class AssemblyOSTable : Table<AssemblyOSRow>
    {
        internal readonly int OSPlatformIDOffset;
        internal readonly int OSMajorVersionOffset;
        internal readonly int OSMinorVersionOffset;

        internal AssemblyOSTable(int numRows, in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            OSPlatformIDOffset = 0;
            OSMajorVersionOffset = OSPlatformIDOffset + sizeof(int);
            OSMinorVersionOffset = OSMajorVersionOffset + sizeof(int);
            RowSize = OSMinorVersionOffset + sizeof(int);
        }

        public int GetOSPlatformID(AssemblyOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSPlatformIDOffset);
        }

        public int GetOSMajorVersion(AssemblyOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSMajorVersionOffset);
        }

        public int GetOSMinorVersion(AssemblyOSIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + OSMinorVersionOffset);
        }

        public int GetRowOffset(AssemblyOSIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyOSRow this[AssemblyOSIndex index] => GetRow((int) index);

        protected override AssemblyOSRow GetRow(int index) => new AssemblyOSRow((AssemblyOSIndex) index, this);
    }
}
