using System;

namespace PESpy.Ecma335
{
    public sealed class MethodSpecTable : Table<MethodSpecRow>
    {
        internal readonly int RowSize;

        internal readonly int MethodOffset;
        internal readonly int InstantiationOffset;

        private readonly bool isBigMethodDefOrRefIndex;
        private readonly bool isBigBlobIndex;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal MethodSpecTable(int numRows, int methodDefOrRefIndexSize, int blobIndexSize, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigMethodDefOrRefIndex = methodDefOrRefIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            MethodOffset = 0;
            InstantiationOffset = MethodOffset + methodDefOrRefIndexSize;
            RowSize = InstantiationOffset + blobIndexSize;
        }

        public Index GetMethod(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, isBigMethodDefOrRefIndex);
        }

        public BlobIndex GetInstantiation(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + InstantiationOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(MethodSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSpecRow this[MethodSpecIndex index] => this[(int) index];

        protected override MethodSpecRow GetRow(int index) => new MethodSpecRow((MethodSpecIndex) index, this);
    }
}
