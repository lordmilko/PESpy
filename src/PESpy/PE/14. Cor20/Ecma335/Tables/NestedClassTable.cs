namespace PESpy.Ecma335
{
    public sealed class NestedClassTable : Table<NestedClassRow>
    {
        private readonly bool isBigTypeDefIndex;

        internal readonly int NestedClassOffset;
        internal readonly int EnclosingClassOffset;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal NestedClassTable(
            int numRows,
            int typeDefIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

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

        internal TypeDefIndex FindEnclosingType(TypeDefIndex nestedTypeDef)
        {
            var rowNumber = CompressedModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                NestedClassOffset,
                (uint) nestedTypeDef.RowId,
                isBigTypeDefIndex
            );

            if (rowNumber == -1)
                return default;

            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowNumber * RowSize + EnclosingClassOffset, isBigTypeDefIndex);
        }

        public int GetRowOffset(NestedClassIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public NestedClassRow this[NestedClassIndex index] => GetRow((int) index);

        protected override NestedClassRow GetRow(int index) => new NestedClassRow((NestedClassIndex) index, this);
    }
}
