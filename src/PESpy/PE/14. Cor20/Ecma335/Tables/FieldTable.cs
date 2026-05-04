using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class FieldTable : Table<FieldRow>
    {
        internal readonly int FlagsOffset;
        internal readonly int NameOffset;
        internal readonly int SignatureOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal FieldTable(
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
            SignatureOffset = NameOffset + stringIndexSize;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public CorFieldAttr GetFlags(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorFieldAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        internal void GetRange(TypeDefIndex typeDef, out int firstFieldRowId, out int lastFieldRowId)
        {
            firstFieldRowId = (int) CompressedModelHeap.TypeDefTable.GetFieldList(typeDef);

            if (firstFieldRowId == 0)
            {
                firstFieldRowId = 1;
                lastFieldRowId = 0;
            }
            else if (typeDef.RowId == CompressedModelHeap.TypeDefTable.Count)
            {
                lastFieldRowId = (CompressedModelHeap.FieldPtrTable?.Count > 0 ? CompressedModelHeap.FieldPtrTable.Count : Count) + 1;
            }
            else
            {
                lastFieldRowId = (int) CompressedModelHeap.TypeDefTable.GetFieldList((TypeDefIndex) (typeDef.RowId + 1));
            }
        }

        public CustomAttributeList GetCustomAttributes(FieldIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Field));

        public long GetRowOffset(FieldIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldRow this[FieldIndex index] => GetRowSafe((int) index);

        protected override FieldRow GetRow(int index) => new FieldRow((FieldIndex) index, this);
    }
}
