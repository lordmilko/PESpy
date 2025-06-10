namespace PESpy.Ecma335
{
    public sealed class FieldPtrTable : Table<FieldPtrRow>
    {
        internal readonly int RowSize;

        private readonly int FieldOffset;

        private readonly bool isBigFieldIndex;

        private readonly MemoryChunk tableChunk;

        internal FieldPtrTable(int numRows, int fieldIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigFieldIndex = fieldIndexSize == 4;

            FieldOffset = 0;
            RowSize = FieldOffset + fieldIndexSize;
        }

        public FieldIndex GetField(FieldPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldOffset, isBigFieldIndex);
        }

        public int GetRowOffset(FieldPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldPtrRow this[FieldPtrIndex index] => this[(int) index];

        protected override FieldPtrRow GetRow(int index) => new FieldPtrRow((FieldPtrIndex) index, this);
    }
}
