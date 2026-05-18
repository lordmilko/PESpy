namespace PESpy.Ecma335
{
    public sealed class ClassLayoutTable : Table<ClassLayoutRow>
    {
        internal readonly int PackingSizeOffset;
        internal readonly int ClassSizeOffset;
        internal readonly int ParentOffset;

        private readonly bool isBigTypeDefIndex;

        internal readonly ModelHeap ModelHeap;

        internal ClassLayoutTable(
            int numRows,
            int typeDefIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

            isBigTypeDefIndex = typeDefIndexSize == 4;

            PackingSizeOffset = 0;
            ClassSizeOffset = PackingSizeOffset + sizeof(ushort);
            ParentOffset = ClassSizeOffset + sizeof(int);
            RowSize = ParentOffset + typeDefIndexSize;
        }

        public short GetPackingSize(ClassLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + PackingSizeOffset);
        }

        public int GetClassSize(ClassLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ClassSizeOffset);
        }

        public TypeDefIndex GetParent(ClassLayoutIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigTypeDefIndex);
        }

        internal ClassLayoutIndex FindRow(TypeDefIndex index)
        {
            return (ClassLayoutIndex) (1 + ModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) index.RowId,
                isBigTypeDefIndex
            ));
        }

        public long GetRowOffset(ClassLayoutIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ClassLayoutRow this[ClassLayoutIndex index] => GetRowSafe((int) index);

        protected override ClassLayoutRow GetRow(int index) => new ClassLayoutRow((ClassLayoutIndex) index, this);
    }
}
