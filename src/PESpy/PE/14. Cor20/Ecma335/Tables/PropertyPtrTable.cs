namespace PESpy.Ecma335
{
    public sealed class PropertyPtrTable : Table<PropertyPtrRow>
    {
        internal readonly int PropertyOffset;

        private readonly bool isBigPropertyIndex;

        internal readonly ModelHeap ModelHeap;

        internal PropertyPtrTable(
            int numRows,
            int propertyIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

            isBigPropertyIndex = propertyIndexSize == 4;

            PropertyOffset = 0;
            RowSize = PropertyOffset + propertyIndexSize;
        }

        public PropertyIndex GetProperty(PropertyPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (PropertyIndex) tableChunk.PeekEcmaIndex(rowOffset + PropertyOffset, isBigPropertyIndex);
        }

        public long GetRowOffset(PropertyPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyPtrRow this[PropertyPtrIndex index] => GetRowSafe((int) index);

        protected override PropertyPtrRow GetRow(int index) => new PropertyPtrRow((PropertyPtrIndex) index, this);
    }
}
