using System;

namespace PESpy.Ecma335
{
    public sealed class TypeSpecTable : Table<TypeSpecRow>
    {
        internal readonly int RowSize;

        internal readonly int SignatureOffset;

        private readonly bool isBigBlobIndex;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal TypeSpecTable(int numRows, int blobIndexSize, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
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

        public int GetRowOffset(TypeSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeSpecRow this[TypeSpecIndex index] => this[(int) index];

        protected override TypeSpecRow GetRow(int index) => new TypeSpecRow((TypeSpecIndex) index, this);
    }
}
