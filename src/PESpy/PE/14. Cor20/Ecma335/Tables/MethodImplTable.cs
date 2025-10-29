namespace PESpy.Ecma335
{
    public sealed class MethodImplTable : Table<MethodImplRow>
    {
        internal readonly int RowSize;

        internal readonly int ClassOffset;
        internal readonly int MethodBodyOffset;
        internal readonly int MethodDeclarationOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigMethodDefOrRefIndex;

        private readonly MemoryChunk tableChunk;

        internal MethodImplTable(int numRows, int typeDefIndexSize, int methodDefOrRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            //II.22.27

            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;
            isBigMethodDefOrRefIndex = methodDefOrRefIndexSize == 4;

            ClassOffset = 0;
            MethodBodyOffset = ClassOffset + typeDefIndexSize;
            MethodDeclarationOffset = MethodBodyOffset + methodDefOrRefIndexSize;
            RowSize = MethodDeclarationOffset + methodDefOrRefIndexSize;
        }

        public TypeDefIndex GetClass(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + ClassOffset, isBigTypeDefIndex);
        }

        public CodedIndex GetMethodBody(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + MethodBodyOffset, isBigMethodDefOrRefIndex, CodedIndexType.MethodDefOrRef);
        }

        public CodedIndex GetMethodDeclaration(MethodImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + MethodDeclarationOffset, isBigMethodDefOrRefIndex, CodedIndexType.MethodDefOrRef);
        }

        public int GetRowOffset(MethodImplIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodImplRow this[MethodImplIndex index] => this[(int) index];

        protected override MethodImplRow GetRow(int index) => new MethodImplRow((MethodImplIndex) index, this);
    }
}
