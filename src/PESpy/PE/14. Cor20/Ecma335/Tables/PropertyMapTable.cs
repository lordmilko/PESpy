namespace PESpy.Ecma335
{
    public sealed class PropertyMapTable : Table<PropertyMapRow>
    {
        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigPropertyIndex;

        internal readonly int ParentOffset;
        internal readonly int PropertyListOffset;

        internal readonly ModelHeap ModelHeap;

        internal PropertyMapTable(
            int numRows,
            int typeDefIndexSize,
            int propertyIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

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

        internal TypeDefRow? FindTypeContainingProperty(int propertyRowId, int numberOfProperties)
        {
            var numberOfRows = Count;

            var row = ModelHeap.BinarySearchEcmaIndexList(
                tableChunk,
                numberOfRows,
                RowSize,
                PropertyListOffset,
                (uint) propertyRowId,
                isBigPropertyIndex
            ) + 1;

            if (row == 0)
                return default;

            if (row > numberOfRows)
            {
                if (propertyRowId <= numberOfProperties)
                    return ModelHeap.TypeDefTable[GetParent((PropertyMapIndex) numberOfRows)];

                return default;
            }

            return ModelHeap.TypeDefTable[GetParent((PropertyMapIndex) row)];
        }

        internal int FindPropertyMapRowIdFor(TypeDefIndex typeDef)
        {
            var rowNumber = ModelHeap.LinearSearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) typeDef.RowId,
                isBigTypeDefIndex
            );

            return rowNumber + 1;
        }

        public long GetRowOffset(PropertyMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyMapRow this[PropertyMapIndex index] => GetRowSafe((int) index);

        protected override PropertyMapRow GetRow(int index) => new PropertyMapRow((PropertyMapIndex) index, this);
    }
}
