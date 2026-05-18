using System;

namespace PESpy.Ecma335
{
    public sealed class StandAloneSigTable : Table<StandAloneSigRow>
    {
        internal readonly int SignatureOffset;

        private readonly bool isBigBlobIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal StandAloneSigTable(
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

        public BlobIndex GetSignature(StandAloneSigIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public CustomAttributeList GetCustomAttributes(StandAloneSigIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.StandAloneSig));

        public long GetRowOffset(StandAloneSigIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public StandAloneSigRow this[StandAloneSigIndex index] => GetRowSafe((int) index);

        protected override StandAloneSigRow GetRow(int index) => new StandAloneSigRow((StandAloneSigIndex) index, this);
    }
}
