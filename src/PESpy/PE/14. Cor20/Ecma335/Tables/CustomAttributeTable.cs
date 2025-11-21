using System;

namespace PESpy.Ecma335
{
    public sealed class CustomAttributeTable : Table<CustomAttributeRow>
    {
        internal readonly int RowSize;

        internal readonly int ParentOffset;
        internal readonly int TypeOffset;
        internal readonly int ValueOffset;

        private readonly bool isBigHasCustomAttributeIndexSize;
        private readonly bool isBigCustomAttributeTypeIndexSize;
        private readonly bool isBigBlobIndexSize;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        //System.Reflection.Metadata calls this "PtrTable" which I think is a confusing name
        internal readonly int[]? SortedTable;

        internal CustomAttributeTable(
            int numRows,
            bool isSorted,
            int hasCustomAttributeIndexSize,
            int customAttributeTypeIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            CompressedModelHeap = compressedModelHeap;
            this.blobHeap = blobHeap;

            isBigHasCustomAttributeIndexSize = hasCustomAttributeIndexSize == 4;
            isBigCustomAttributeTypeIndexSize = customAttributeTypeIndexSize == 4;
            isBigBlobIndexSize = blobIndexSize == 4;

            ParentOffset = 0;
            TypeOffset = ParentOffset + hasCustomAttributeIndexSize;
            ValueOffset = TypeOffset + customAttributeTypeIndexSize;
            RowSize = ValueOffset + blobIndexSize;

            if (!isSorted)
            {
                throw new NotImplementedException("Checking whether we're already sorted, and if not manually sorting us, is not implemented");
            }
        }

        public int GetRowOffset(CustomAttributeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public CodedIndex GetParent(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ParentOffset, isBigHasCustomAttributeIndexSize, CodedIndexType.HasCustomAttribute);
        }

        public CodedIndex GetType(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + TypeOffset, isBigCustomAttributeTypeIndexSize, CodedIndexType.CustomAttributeType);
        }

        public BlobIndex GetValue(CustomAttributeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndexSize), blobHeap);
        }

        internal void GetRange(CodedIndex index, out int firstRowId, out int lastRowId)
        {
            if (SortedTable != null)
                throw new NotImplementedException("Getting the range from the sorted table is not implemented");

            CompressedModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) (int) index,
                isBigHasCustomAttributeIndexSize,
                out var startRowNumber,
                out var endRowNumber
            );

            if (startRowNumber == -1)
            {
                firstRowId = 1;
                lastRowId = 0;
            }
            else
            {
                firstRowId = startRowNumber + 1;
                lastRowId = endRowNumber + 1;
            }
        }

        public CustomAttributeRow this[CustomAttributeIndex index] => this[(int) index];

        protected override CustomAttributeRow GetRow(int index) => new CustomAttributeRow((CustomAttributeIndex) index, this);
    }
}
