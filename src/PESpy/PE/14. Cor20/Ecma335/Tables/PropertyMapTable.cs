namespace PESpy.Ecma335
{
    public sealed class PropertyMapTable : Table<PropertyMapRow>
    {
        internal readonly int RowSize;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigPropertyIndex;

        internal readonly int ParentOffset;
        internal readonly int PropertyListOffset;

        private readonly MemoryChunk tableChunk;

        internal PropertyMapTable(int numRows, int typeDefIndexSize, int propertyIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;
            isBigPropertyIndex = propertyIndexSize == 4;

            ParentOffset = 0;
            PropertyListOffset = ParentOffset + typeDefIndexSize;
            RowSize = PropertyListOffset + propertyIndexSize;
        }

        public TypeDefIndex GetParent(PropertyMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigTypeDefIndex);
        }

        public PropertyIndex GetPropertyList(PropertyMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (PropertyIndex) tableChunk.PeekEcmaIndex(rowOffset + PropertyListOffset, isBigPropertyIndex);
        }

        public int GetRowOffset(PropertyMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyMapRow this[PropertyMapIndex index] => this[(int) index];

        protected override PropertyMapRow GetRow(int index) => new PropertyMapRow((PropertyMapIndex) index, this);
    }
}
