using System;

namespace PESpy.Ecma335
{
    public sealed class FieldMarshalTable : Table<FieldMarshalRow>
    {
        internal readonly int ParentOffset;
        internal readonly int NativeTypeOffset;

        private readonly bool isBigHasFieldMarshalIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal FieldMarshalTable(
            int numRows,
            int hasFieldMarshalIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.17

            CompressedModelHeap = compressedModelHeap;
            this.blobHeap = blobHeap;

            isBigHasFieldMarshalIndex = hasFieldMarshalIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            ParentOffset = 0;
            NativeTypeOffset = ParentOffset + hasFieldMarshalIndexSize;
            RowSize = NativeTypeOffset + blobIndexSize;
        }

        public CodedIndex GetParent(FieldMarshalIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ParentOffset, isBigHasFieldMarshalIndex, CodedIndexType.HasFieldMarshal);
        }

        public BlobIndex GetNativeType(FieldMarshalIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + NativeTypeOffset, isBigBlobIndex), blobHeap);
        }

        internal FieldMarshalIndex FindFieldMarshalRowId(CodedIndex index)
        {
            var foundRowNumber = CompressedModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) (int) index,
                isBigHasFieldMarshalIndex
            );

            return (FieldMarshalIndex) (foundRowNumber + 1);
        }

        public int GetRowOffset(FieldMarshalIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldMarshalRow this[FieldMarshalIndex index] => GetRow((int) index);

        protected override FieldMarshalRow GetRow(int index) => new FieldMarshalRow((FieldMarshalIndex) index, this);
    }
}
