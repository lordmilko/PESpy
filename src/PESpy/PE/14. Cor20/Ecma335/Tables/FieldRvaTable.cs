namespace PESpy.Ecma335
{
    public sealed class FieldRvaTable : Table<FieldRvaRow>
    {
        internal readonly int RVAOffset;
        internal readonly int FieldOffset;

        private readonly bool isBigFieldIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal FieldRvaTable(
            int numRows,
            int fieldIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigFieldIndex = fieldIndexSize == 4;

            RVAOffset = 0;
            FieldOffset = RVAOffset + sizeof(int);
            RowSize = FieldOffset + fieldIndexSize;
        }

        public int GetRVA(FieldRvaIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + RVAOffset);
        }

        public FieldIndex GetField(FieldRvaIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldOffset, isBigFieldIndex);
        }

        internal FieldRvaIndex FindFieldRvaRowId(int fieldDefRowId)
        {
            var foundRowNumber = CompressedModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                FieldOffset,
                (uint) fieldDefRowId,
                isBigFieldIndex
            );

            return (FieldRvaIndex) (foundRowNumber + 1);
        }

        public long GetRowOffset(FieldRvaIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldRvaRow this[FieldRvaIndex index] => GetRowSafe((int) index);

        protected override FieldRvaRow GetRow(int index) => new FieldRvaRow((FieldRvaIndex) index, this);
    }
}
