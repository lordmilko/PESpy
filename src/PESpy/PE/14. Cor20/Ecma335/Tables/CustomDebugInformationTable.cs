using System;

namespace PESpy.Ecma335
{
    public sealed class CustomDebugInformationTable : Table<CustomDebugInformationRow>
    {
        internal readonly int RowSize;

        private readonly int ParentOffset;
        private readonly int KindOffset;
        private readonly int ValueOffset;

        private readonly bool isBigHasCustomDebugInformationIndex;
        private readonly bool isBigGuidIndex;
        private readonly bool isBigBlobIndex;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly Lazy<GuidHeap?> guidHeap;
        private readonly MemoryChunk tableChunk;

        internal CustomDebugInformationTable(int numRows, int hasCustomDebugInformationIndexSize, int guidIndexSize, int blobIndexSize, Lazy<BlobHeap?> blobHeap, Lazy<GuidHeap?> guidHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;
            this.guidHeap = guidHeap;

            isBigHasCustomDebugInformationIndex = hasCustomDebugInformationIndexSize == 4;
            isBigGuidIndex = guidIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            ParentOffset = 0;
            KindOffset = ParentOffset + hasCustomDebugInformationIndexSize;
            ValueOffset = KindOffset + guidIndexSize;
            RowSize = ValueOffset + blobIndexSize;
        }

        public int GetParent(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigHasCustomDebugInformationIndex);
        }

        public GuidIndex GetKind(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + KindOffset, isBigGuidIndex), guidHeap.Value);
        }

        public BlobIndex GetValue(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(CustomDebugInformationIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public CustomDebugInformationRow this[CustomDebugInformationIndex index] => this[(int) index];

        protected override CustomDebugInformationRow GetRow(int index) => new CustomDebugInformationRow((CustomDebugInformationIndex) index, this);
    }
}
