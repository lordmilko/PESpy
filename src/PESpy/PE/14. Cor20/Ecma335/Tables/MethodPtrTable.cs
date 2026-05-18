namespace PESpy.Ecma335
{
    public sealed class MethodPtrTable : Table<MethodPtrRow>
    {
        internal readonly int MethodOffset;

        private readonly bool isBigMethodIndex;

        internal readonly ModelHeap ModelHeap;

        internal MethodPtrTable(
            int numRows,
            int methodIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

            isBigMethodIndex = methodIndexSize == 4;

            MethodOffset = 0;
            RowSize = MethodOffset + methodIndexSize;
        }

        public MethodDefIndex GetMethod(MethodPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, isBigMethodIndex);
        }

        public long GetRowOffset(MethodPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodPtrRow this[MethodPtrIndex index] => GetRowSafe((int) index);

        protected override MethodPtrRow GetRow(int index) => new MethodPtrRow((MethodPtrIndex) index, this);
    }
}
