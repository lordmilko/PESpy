using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ManifestResourceTable : Table<ManifestResourceRow>
    {
        internal readonly int RowSize;

        internal readonly int ResourceOffsetOffset;
        internal readonly int FlagsOffset;
        internal readonly int NameOffset;
        internal readonly int ImplementationOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigImplementationIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal ManifestResourceTable(int numRows, int stringIndexSize, int implementationIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigImplementationIndex = implementationIndexSize == 4;

            ResourceOffsetOffset = 0;
            FlagsOffset = ResourceOffsetOffset + sizeof(int);
            NameOffset = FlagsOffset + sizeof(int);
            ImplementationOffset = NameOffset + stringIndexSize;
            RowSize = ImplementationOffset + implementationIndexSize;
        }

        public int GetResourceOffset(ManifestResourceIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ResourceOffsetOffset);
        }

        public CorManifestResourceFlags GetFlags(ManifestResourceIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorManifestResourceFlags) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(ManifestResourceIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public Index GetImplementation(ManifestResourceIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + ImplementationOffset, isBigImplementationIndex);
        }

        public int GetRowOffset(ManifestResourceIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ManifestResourceRow this[ManifestResourceIndex index] => this[(int) index];

        protected override ManifestResourceRow GetRow(int index) => new ManifestResourceRow((ManifestResourceIndex) index, this);
    }
}
