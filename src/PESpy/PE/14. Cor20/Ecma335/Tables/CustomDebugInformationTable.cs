using System;

namespace PESpy.Ecma335
{
    public sealed class CustomDebugInformationTable : Table<CustomDebugInformationRow>
    {
        internal readonly int ParentOffset;
        internal readonly int KindOffset;
        internal readonly int ValueOffset;

        private readonly bool isBigHasCustomDebugInformationIndex;
        private readonly bool isBigGuidIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly Func<GuidHeap?> guidHeap;

        internal CustomDebugInformationTable(
            int numRows,
            int hasCustomDebugInformationIndexSize,
            int guidIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<BlobHeap?> blobHeap,
            Func<GuidHeap?> guidHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
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

        public CodedIndex GetParent(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ParentOffset, isBigHasCustomDebugInformationIndex, CodedIndexType.HasCustomDebugInformation);
        }

        public GuidIndex GetKind(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + KindOffset, isBigGuidIndex), guidHeap);
        }

        public BlobIndex GetValue(CustomDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ValueOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(CustomDebugInformationIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public CustomDebugInformationRow this[CustomDebugInformationIndex index] => GetRow((int) index);

        protected override CustomDebugInformationRow GetRow(int index) => new CustomDebugInformationRow((CustomDebugInformationIndex) index, this);
    }
}
