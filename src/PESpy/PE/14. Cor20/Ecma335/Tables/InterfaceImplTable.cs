namespace PESpy.Ecma335
{
    public sealed class InterfaceImplTable : Table<InterfaceImplRow>
    {
        internal readonly int RowSize;

        internal readonly int ClassOffset;
        internal readonly int InterfaceOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigTypeDefOrRefIndex;

        private readonly MemoryChunk tableChunk;

        internal InterfaceImplTable(int numRows, int typeDefIndexSize, int typeDefOrRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;
            isBigTypeDefOrRefIndex = typeDefOrRefIndexSize == 4;

            ClassOffset = 0;
            InterfaceOffset = ClassOffset + typeDefIndexSize;
            RowSize = InterfaceOffset + typeDefOrRefIndexSize;
        }

        public TypeDefIndex GetClass(InterfaceImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + ClassOffset, isBigTypeDefIndex);
        }

        public Index GetInterface(InterfaceImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + InterfaceOffset, isBigTypeDefOrRefIndex);
        }

        public int GetRowOffset(InterfaceImplIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public InterfaceImplRow this[InterfaceImplIndex index] => this[(int) index];

        protected override InterfaceImplRow GetRow(int index) => new InterfaceImplRow((InterfaceImplIndex) index, this);
    }
}
