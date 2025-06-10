using System;

namespace PESpy.Ecma335
{
    public sealed class FieldMarshalTable : Table<FieldMarshalRow>
    {
        internal readonly int RowSize;

        private readonly int ParentOffset;
        private readonly int NativeTypeOffset;

        private readonly bool isBigHasFieldMarshalIndex;
        private readonly bool isBigBlobIndex;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal FieldMarshalTable(int numRows, int hasFieldMarshalIndexSize, int blobIndexSize, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigHasFieldMarshalIndex = hasFieldMarshalIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            ParentOffset = 0;
            NativeTypeOffset = ParentOffset + hasFieldMarshalIndexSize;
            RowSize = NativeTypeOffset + blobIndexSize;
        }

        public Index GetParent(FieldMarshalIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigHasFieldMarshalIndex);
        }

        public BlobIndex GetNativeType(FieldMarshalIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + NativeTypeOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(FieldMarshalIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldMarshalRow this[FieldMarshalIndex index] => this[(int) index];

        protected override FieldMarshalRow GetRow(int index) => new FieldMarshalRow((FieldMarshalIndex) index, this);
    }
}
