namespace PESpy.Ecma335
{
    public sealed class ClassLayoutTable : Table<ClassLayoutRow>
    {
        internal readonly int RowSize;

        internal readonly int PackingSizeOffset;
        internal readonly int ClassSizeOffset;
        internal readonly int ParentOffset;

        private readonly bool isBigTypeDefIndex;

        private readonly MemoryChunk tableChunk;

        internal ClassLayoutTable(int numRows, int typeDefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetRowOffset(ClassLayoutIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ClassLayoutRow this[ClassLayoutIndex index] => this[(int) index];

        protected override ClassLayoutRow GetRow(int index) => new ClassLayoutRow((ClassLayoutIndex) index, this);
    }
}
