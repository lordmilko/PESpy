using System;

namespace PESpy.Ecma335
{
    public sealed class LocalConstantTable : Table<LocalConstantRow>
    {
        internal readonly int RowSize;

        internal readonly int NameOffset;
        internal readonly int SignatureOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal LocalConstantTable(int numRows, int stringIndexSize, int blobIndexSize, Func<StringHeap?> stringHeap, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            NameOffset = 0;
            SignatureOffset = NameOffset + stringIndexSize;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public StringIndex GetName(LocalConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(LocalConstantIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(LocalConstantIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public LocalConstantRow this[LocalConstantIndex index] => this[(int) index];

        protected override LocalConstantRow GetRow(int index) => new LocalConstantRow((LocalConstantIndex) index, this);
    }
}
