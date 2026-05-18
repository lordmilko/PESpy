namespace PESpy.Ecma335
{
    public sealed class MethodImplTable : Table<MethodImplRow>
    {
        internal readonly int ClassOffset;
        internal readonly int MethodBodyOffset;
        internal readonly int MethodDeclarationOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigMethodDefOrRefIndex;

        internal readonly ModelHeap ModelHeap;

        internal MethodImplTable(
            int numRows,
            int typeDefIndexSize,
            int methodDefOrRefIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.27

            ModelHeap = modelHeap;

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

        internal void GetRange(TypeDefIndex typeDef, out int firstImplRowId, out int lastImplRowId)
        {
            ModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                ClassOffset,
                (uint) typeDef.RowId,
                isBigTypeDefIndex,
                out var startRowNumber,
                out var endRowNumber
            );

            if (startRowNumber == -1)
            {
                firstImplRowId = 0;
                lastImplRowId = 0;
            }
            else
            {
                firstImplRowId = startRowNumber + 1;
                lastImplRowId = endRowNumber + 2; //+1 gets us the actual last row and we want +2 to be +1 past it
            }
        }

        public long GetRowOffset(MethodImplIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodImplRow this[MethodImplIndex index] => GetRowSafe((int) index);

        protected override MethodImplRow GetRow(int index) => new MethodImplRow((MethodImplIndex) index, this);
    }
}
