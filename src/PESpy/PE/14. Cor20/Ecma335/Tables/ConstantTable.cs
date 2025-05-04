using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ConstantTable : Table<ConstantRow>
    {
        internal readonly int RowSize;

        private readonly int TypeOffset;
        private readonly int PaddingOffset;
        private readonly int ParentOffset;
        private readonly int ValueOffset;

        private readonly bool isBigHasConstantIndexSize;
        private readonly bool isBigBlobIndexSize;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal ConstantTable(int numRows, int hasConstantIndexSize, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigHasConstantIndexSize = hasConstantIndexSize == 4;
            isBigBlobIndexSize = blobIndexSize == 4;

            TypeOffset = 0;
            PaddingOffset = TypeOffset + sizeof(byte);
            ParentOffset = PaddingOffset + sizeof(byte);
            ValueOffset = ParentOffset + hasConstantIndexSize;
            RowSize = ValueOffset + blobIndexSize;
        }

        public CorElementType GetType(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorElementType) tableChunk.PeekByte(rowOffset + TypeOffset);
        }

        public byte GetPadding(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekByte(rowOffset + PaddingOffset);
        }

        public int GetParent(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigHasConstantIndexSize);
        }

        public BlobIndex GetValue(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndexSize), blobHeap.Value);
        }

        public int GetRowOffset(ConstantIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ConstantRow this[ConstantIndex index] => this[(int) index];

        protected override ConstantRow GetRow(int index) => new ConstantRow((ConstantIndex) index, this);
    }
}
