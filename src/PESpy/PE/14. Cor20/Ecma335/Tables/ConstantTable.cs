using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ConstantTable : Table<ConstantRow>
    {
        internal readonly int TypeOffset;
        internal readonly int PaddingOffset;
        internal readonly int ParentOffset;
        internal readonly int ValueOffset;

        private readonly bool isBigHasConstantIndexSize;
        private readonly bool isBigBlobIndexSize;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal ConstantTable(
            int numRows,
            int hasConstantIndexSize,
            int blobIndexSize,
            ModelHeap modelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;
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

        public CodedIndex GetParent(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ParentOffset, isBigHasConstantIndexSize, CodedIndexType.HasConstant);
        }

        public BlobIndex GetValue(ConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndexSize), blobHeap);
        }

        internal ConstantIndex FindConstant(CodedIndex index)
        {
            var foundRowNumber = ModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) (int) index,
                isBigHasConstantIndexSize
            );

            return (ConstantIndex) (foundRowNumber + 1);
        }

        public long GetRowOffset(ConstantIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ConstantRow this[ConstantIndex index] => GetRowSafe((int) index);

        protected override ConstantRow GetRow(int index) => new ConstantRow((ConstantIndex) index, this);
    }
}
