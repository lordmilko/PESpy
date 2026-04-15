using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class PropertyTable : Table<PropertyRow>
    {
        internal readonly int FlagsOffset;
        internal readonly int NameOffset;
        internal readonly int TypeOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal PropertyTable(
            int numRows,
            int stringIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            FlagsOffset = 0;
            NameOffset = FlagsOffset + sizeof(ushort);
            TypeOffset = NameOffset + stringIndexSize;
            RowSize = TypeOffset + blobIndexSize;
        }

        public CorPropertyAttr GetFlags(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorPropertyAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetType(PropertyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeOffset, isBigBlobIndex), blobHeap);
        }

        internal void GetRange(TypeDefIndex typeDef, out int firstPropertyRowId, out int lastPropertyRowId)
        {
            var propertyMapRowId = CompressedModelHeap.PropertyMapTable.FindPropertyMapRowIdFor(typeDef);

            if (propertyMapRowId == 0)
            {
                firstPropertyRowId = 0;
                lastPropertyRowId = 0;
                return;
            }

            firstPropertyRowId = (int) CompressedModelHeap.PropertyMapTable.GetPropertyList((PropertyMapIndex) propertyMapRowId);

            if (propertyMapRowId == CompressedModelHeap.PropertyMapTable.Count)
            {
                lastPropertyRowId = (CompressedModelHeap.PropertyPtrTable?.Count > 0 ? CompressedModelHeap.PropertyPtrTable.Count : Count) + 1;
            }
            else
            {
                lastPropertyRowId = (int) CompressedModelHeap.PropertyMapTable.GetPropertyList((PropertyMapIndex) (propertyMapRowId + 1));
            }
        }

        public CustomAttributeList GetCustomAttributes(PropertyIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Property));

        public int GetRowOffset(PropertyIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public PropertyRow this[PropertyIndex index] => GetRowSafe((int) index);

        protected override PropertyRow GetRow(int index) => new PropertyRow((PropertyIndex) index, this);
    }
}
