namespace PESpy.Ecma335
{
    public sealed class NestedClassTable : Table<NestedClassRow>
    {
        internal readonly int RowSize;

        private readonly bool isBigTypeDefIndex;

        internal readonly int NestedClassOffset;
        internal readonly int EnclosingClassOffset;

        private readonly MemoryChunk tableChunk;

        internal NestedClassTable(int numRows, int typeDefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;

            NestedClassOffset = 0;
            EnclosingClassOffset = NestedClassOffset + typeDefIndexSize;
            RowSize = EnclosingClassOffset + typeDefIndexSize;
        }

        public TypeDefIndex GetNestedClass(NestedClassIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + NestedClassOffset, isBigTypeDefIndex);
        }

        public TypeDefIndex GetEnclosingClass(NestedClassIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + EnclosingClassOffset, isBigTypeDefIndex);
        }

        public int GetRowOffset(NestedClassIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public NestedClassRow this[NestedClassIndex index] => this[(int) index];

        protected override NestedClassRow GetRow(int index) => new NestedClassRow((NestedClassIndex) index, this);
    }
}
