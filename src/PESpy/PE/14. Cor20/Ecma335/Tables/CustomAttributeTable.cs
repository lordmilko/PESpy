using System;

namespace PESpy.Ecma335
{
    public sealed class CustomAttributeTable : Table<CustomAttributeRow>
    {
        internal readonly int RowSize;

        private readonly int ParentOffset;
        private readonly int TypeOffset;
        private readonly int ValueOffset;

        private readonly bool isBigHasCustomAttributeIndexSize;
        private readonly bool isBigCustomAttributeTypeIndexSize;
        private readonly bool isBigBlobIndexSize;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal CustomAttributeTable(int numRows, int hasCustomAttributeIndexSize, int customAttributeTypeIndexSize, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigHasCustomAttributeIndexSize = hasCustomAttributeIndexSize == 4;
            isBigCustomAttributeTypeIndexSize = customAttributeTypeIndexSize == 4;
            isBigBlobIndexSize = blobIndexSize == 4;

            ParentOffset = 0;
            TypeOffset = ParentOffset + hasCustomAttributeIndexSize;
            ValueOffset = TypeOffset + customAttributeTypeIndexSize;
            RowSize = ValueOffset + blobIndexSize;
        }

        public int GetRowOffset(CustomAttributeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public int GetParent(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigHasCustomAttributeIndexSize);
        }

        public int GetType(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + TypeOffset, isBigCustomAttributeTypeIndexSize);
        }

        public BlobIndex GetValue(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndexSize), blobHeap.Value);
        }

        public CustomAttributeRow this[CustomAttributeIndex index] => this[(int) index];

        protected override CustomAttributeRow GetRow(int index) => new CustomAttributeRow((CustomAttributeIndex) index, this);
    }
}
