namespace PESpy.Ecma335
{
    public sealed class PropertyPtrTable : Table<PropertyPtrRow>
    {
        internal readonly int RowSize;

        internal readonly int PropertyOffset;

        private readonly bool isBigPropertyIndex;

        private readonly MemoryChunk tableChunk;

        internal PropertyPtrTable(int numRows, int propertyIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigPropertyIndex = propertyIndexSize == 4;

            PropertyOffset = 0;
            RowSize = PropertyOffset + propertyIndexSize;
        }

        public PropertyIndex GetProperty(PropertyPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (PropertyIndex) tableChunk.PeekEcmaIndex(rowOffset + PropertyOffset, isBigPropertyIndex);
        }

        public int GetRowOffset(PropertyPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyPtrRow this[PropertyPtrIndex index] => this[(int) index];

        protected override PropertyPtrRow GetRow(int index) => new PropertyPtrRow((PropertyPtrIndex) index, this);
    }
}
