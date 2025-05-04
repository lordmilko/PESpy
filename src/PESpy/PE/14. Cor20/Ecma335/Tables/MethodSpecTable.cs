using System;

namespace PESpy.Ecma335
{
    public sealed class MethodSpecTable : Table<MethodSpecRow>
    {
        internal readonly int RowSize;

        private readonly int MethodOffset;
        private readonly int InstantiationOffset;

        private readonly bool isBigMethodDefOrRefIndex;
        private readonly bool isBigBlobIndex;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal MethodSpecTable(int numRows, int methodDefOrRefIndexSize, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigMethodDefOrRefIndex = methodDefOrRefIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            MethodOffset = 0;
            InstantiationOffset = MethodOffset + methodDefOrRefIndexSize;
            RowSize = InstantiationOffset + blobIndexSize;
        }

        public int GetMethod(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, isBigMethodDefOrRefIndex);
        }

        public BlobIndex GetInstantiation(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + InstantiationOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(MethodSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSpecRow this[MethodSpecIndex index] => this[(int) index];

        protected override MethodSpecRow GetRow(int index) => new MethodSpecRow((MethodSpecIndex) index, this);
    }
}
