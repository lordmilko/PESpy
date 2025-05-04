namespace PESpy.Ecma335
{
    public sealed class FieldLayoutTable : Table<FieldLayoutRow>
    {
        internal readonly int RowSize;

        private readonly int FieldOffsetOffset;
        private readonly int FieldOffset;

        private readonly bool isBigFieldIndexSize;

        private readonly MemoryChunk tableChunk;

        internal FieldLayoutTable(int numRows, int fieldIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetField(FieldLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + FieldOffset, isBigFieldIndexSize);
        }

        public int GetRowOffset(FieldLayoutIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldLayoutRow this[FieldLayoutIndex index] => this[(int) index];

        protected override FieldLayoutRow GetRow(int index) => new FieldLayoutRow((FieldLayoutIndex) index, this);
    }
}
