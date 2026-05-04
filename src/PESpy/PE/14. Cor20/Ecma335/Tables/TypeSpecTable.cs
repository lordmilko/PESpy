using System;

namespace PESpy.Ecma335
{
    public sealed class TypeSpecTable : Table<TypeSpecRow>
    {
        internal readonly int SignatureOffset;

        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal TypeSpecTable(
            int numRows,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
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
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.TypeSpec));

        public long GetRowOffset(TypeSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeSpecRow this[TypeSpecIndex index] => GetRowSafe((int) index);

        protected override TypeSpecRow GetRow(int index) => new TypeSpecRow((TypeSpecIndex) index, this);
    }
}
