namespace PESpy.Ecma335
{
    public sealed class MethodPtrTable : Table<MethodPtrRow>
    {
        internal readonly int RowSize;

        private readonly int MethodOffset;

        private readonly bool isBigMethodIndex;

        private readonly MemoryChunk tableChunk;

        internal MethodPtrTable(int numRows, int methodIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigMethodIndex = methodIndexSize == 4;

            MethodOffset = 0;
            RowSize = MethodOffset + methodIndexSize;
        }

        public int GetMethod(MethodPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, isBigMethodIndex);
        }

        public int GetRowOffset(MethodPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodPtrRow this[MethodPtrIndex index] => this[(int) index];

        protected override MethodPtrRow GetRow(int index) => new MethodPtrRow((MethodPtrIndex) index, this);
    }
}
