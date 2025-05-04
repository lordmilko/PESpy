using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class PropertyTable : Table<PropertyRow>
    {
        internal readonly int RowSize;

        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int TypeOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal PropertyTable(int numRows, int stringIndexSize, int blobIndexSize, Lazy<StringHeap?> stringHeap, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            FlagsOffset = 0;
            NameOffset = FlagsOffset + sizeof(ushort);
            TypeOffset = NameOffset + stringIndexSize;
            RowSize = TypeOffset + blobIndexSize;
        }

        public CorPropertyAttr GetFlags(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorPropertyAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap.Value);
        }

        public BlobIndex GetType(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(PropertyIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyRow this[PropertyIndex index] => this[(int) index];

        protected override PropertyRow GetRow(int index) => new PropertyRow((PropertyIndex) index, this);
    }
}
