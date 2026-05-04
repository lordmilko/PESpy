namespace PESpy.Ecma335
{
    public sealed class FieldPtrTable : Table<FieldPtrRow>
    {
        internal readonly int FieldOffset;

        private readonly bool isBigFieldIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal FieldPtrTable(
            int numRows,
            int fieldIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigFieldIndex = fieldIndexSize == 4;

            FieldOffset = 0;
            RowSize = FieldOffset + fieldIndexSize;
        }

        public FieldIndex GetField(FieldPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldOffset, isBigFieldIndex);
        }

        public long GetRowOffset(FieldPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldPtrRow this[FieldPtrIndex index] => GetRowSafe((int) index);

        protected override FieldPtrRow GetRow(int index) => new FieldPtrRow((FieldPtrIndex) index, this);
    }
}
