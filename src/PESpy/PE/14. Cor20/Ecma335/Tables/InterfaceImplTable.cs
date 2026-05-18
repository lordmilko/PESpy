namespace PESpy.Ecma335
{
    public sealed class InterfaceImplTable : Table<InterfaceImplRow>
    {
        internal readonly int ClassOffset;
        internal readonly int InterfaceOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigTypeDefOrRefIndex;

        internal readonly ModelHeap ModelHeap;

        internal InterfaceImplTable(
            int numRows,
            int typeDefIndexSize,
            int typeDefOrRefIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.23

            ModelHeap = modelHeap;

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

        public CodedIndex GetInterface(InterfaceImplIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + InterfaceOffset, isBigTypeDefOrRefIndex, CodedIndexType.TypeDefOrRef);
        }

        internal void GetRange(TypeDefIndex typeDef, out int firstImplRowId, out int lastImplRowId)
        {
            ModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                ClassOffset,
                (uint) typeDef.RowId,
                isBigTypeDefOrRefIndex,
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

        public CustomAttributeList GetCustomAttributes(InterfaceImplIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.InterfaceImpl));

        public long GetRowOffset(InterfaceImplIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public InterfaceImplRow this[InterfaceImplIndex index] => GetRowSafe((int) index);

        protected override InterfaceImplRow GetRow(int index) => new InterfaceImplRow((InterfaceImplIndex) index, this);
    }
}
