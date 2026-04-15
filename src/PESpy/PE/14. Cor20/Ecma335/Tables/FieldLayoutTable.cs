namespace PESpy.Ecma335
{
    public sealed class FieldLayoutTable : Table<FieldLayoutRow>
    {
        internal readonly int FieldOffsetOffset;
        internal readonly int FieldOffset;

        private readonly bool isBigFieldIndexSize;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal FieldLayoutTable(
            int numRows,
            int fieldIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigFieldIndexSize = fieldIndexSize == 4;

            FieldOffsetOffset = 0;
            FieldOffset = FieldOffsetOffset + sizeof(int);
            RowSize = FieldOffset + fieldIndexSize;
        }

        public int GetFieldOffset(FieldLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + FieldOffsetOffset);
        }

        public FieldIndex GetField(FieldLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldOffset, isBigFieldIndexSize);
        }

        public int GetRowOffset(FieldLayoutIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldLayoutRow this[FieldLayoutIndex index] => GetRowSafe((int) index);

        protected override FieldLayoutRow GetRow(int index) => new FieldLayoutRow((FieldLayoutIndex) index, this);
    }
}
