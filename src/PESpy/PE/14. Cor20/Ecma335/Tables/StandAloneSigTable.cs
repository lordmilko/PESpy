using System;

namespace PESpy.Ecma335
{
    public sealed class StandAloneSigTable : Table<StandAloneSigRow>
    {
        internal readonly int RowSize;

        private readonly int SignatureOffset;

        private readonly bool isBigBlobIndex;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal StandAloneSigTable(int numRows, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;

            SignatureOffset = 0;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public BlobIndex GetSignature(StandAloneSigIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(StandAloneSigIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public StandAloneSigRow this[StandAloneSigIndex index] => this[(int) index];

        protected override StandAloneSigRow GetRow(int index) => new StandAloneSigRow((StandAloneSigIndex) index, this);
    }
}
