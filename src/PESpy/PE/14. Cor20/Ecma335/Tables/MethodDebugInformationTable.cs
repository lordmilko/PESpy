using System;

namespace PESpy.Ecma335
{
    public sealed class MethodDebugInformationTable : Table<MethodDebugInformationRow>
    {
        internal readonly int DocumentOffset;
        internal readonly int SequencePointsOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigDocumentIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal MethodDebugInformationTable(
            int numRows,
            int blobIndexSize,
            int documentIndexSize,
            ModelHeap modelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;
            isBigDocumentIndex = documentIndexSize == 4;

            DocumentOffset = 0;
            SequencePointsOffset = DocumentOffset + documentIndexSize;
            RowSize = SequencePointsOffset + blobIndexSize;
        }

        public DocumentIndex GetDocument(MethodDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (DocumentIndex) tableChunk.PeekEcmaIndex(rowOffset + DocumentOffset, isBigDocumentIndex);
        }

        public BlobIndex GetSequencePoints(MethodDebugInformationIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SequencePointsOffset, isBigBlobIndex), blobHeap);
        }

        public long GetRowOffset(MethodDebugInformationIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodDebugInformationRow this[MethodDebugInformationIndex index] => GetRowSafe((int) index);

        public MethodDebugInformationRow this[MethodDefIndex index] => GetRowSafe((int) index);

        protected override MethodDebugInformationRow GetRow(int index) => new MethodDebugInformationRow((MethodDebugInformationIndex) index, this);
    }
}
