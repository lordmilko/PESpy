namespace PESpy.Ecma335
{
    public sealed class FieldRvaTable : Table<FieldRvaRow>
    {
        internal readonly int RowSize;

        private readonly int RVAOffset;
        private readonly int FieldOffset;

        private readonly bool isBigFieldIndex;

        private readonly MemoryChunk tableChunk;

        internal FieldRvaTable(int numRows, int fieldIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetRowOffset(FieldRvaIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldRvaRow this[FieldRvaIndex index] => this[(int) index];

        protected override FieldRvaRow GetRow(int index) => new FieldRvaRow((FieldRvaIndex) index, this);
    }
}
