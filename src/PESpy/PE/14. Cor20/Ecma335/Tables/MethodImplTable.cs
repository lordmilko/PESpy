namespace PESpy.Ecma335
{
    public sealed class MethodImplTable : Table<MethodImplRow>
    {
        internal readonly int RowSize;

        private readonly int ClassOffset;
        private readonly int MethodBodyOffset;
        private readonly int MethodDeclarationOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigMethodDefOrRefIndex;

        private readonly MemoryChunk tableChunk;

        internal MethodImplTable(int numRows, int typeDefIndexSize, int methodDefOrRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;
            isBigMethodDefOrRefIndex = methodDefOrRefIndexSize == 4;

            ClassOffset = 0;
            MethodBodyOffset = ClassOffset + typeDefIndexSize;
            MethodDeclarationOffset = MethodBodyOffset + methodDefOrRefIndexSize;
            RowSize = MethodDeclarationOffset + methodDefOrRefIndexSize;
        }

        public int GetClass(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ClassOffset, isBigTypeDefIndex);
        }

        public int GetMethodBody(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodBodyOffset, isBigMethodDefOrRefIndex);
        }

        public int GetMethodDeclaration(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodDeclarationOffset, isBigMethodDefOrRefIndex);
        }

        public int GetRowOffset(MethodImplIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodImplRow this[MethodImplIndex index] => this[(int) index];

        protected override MethodImplRow GetRow(int index) => new MethodImplRow((MethodImplIndex) index, this);
    }
}
