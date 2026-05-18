using System;

namespace PESpy.Ecma335
{
    public sealed class TypeSpecTable : Table<TypeSpecRow>
    {
        internal readonly int SignatureOffset;

        private readonly bool isBigBlobIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal TypeSpecTable(
            int numRows,
            int blobIndexSize,
            ModelHeap modelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;

            SignatureOffset = 0;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public BlobIndex GetSignature(TypeSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public CustomAttributeList GetCustomAttributes(TypeSpecIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.TypeSpec));

        public long GetRowOffset(TypeSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeSpecRow this[TypeSpecIndex index] => GetRowSafe((int) index);

        protected override TypeSpecRow GetRow(int index) => new TypeSpecRow((TypeSpecIndex) index, this);
    }
}
