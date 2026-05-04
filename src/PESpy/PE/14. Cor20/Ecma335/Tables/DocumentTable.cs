using System;

namespace PESpy.Ecma335
{
    public sealed class DocumentTable : Table<DocumentRow>
    {
        internal readonly int NameOffset;
        internal readonly int HashAlgorithmOffset;
        internal readonly int HashOffset;
        internal readonly int LanguageOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigGuidIndex;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly Func<GuidHeap?> guidHeap;

        internal DocumentTable(
            int numRows,
            int blobIndexSize,
            int guidIndexSize,
            Func<BlobHeap?> blobHeap,
            Func<GuidHeap?> guidHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            this.blobHeap = blobHeap;
            this.guidHeap = guidHeap;

            isBigBlobIndex = blobIndexSize == 4;
            isBigGuidIndex = guidIndexSize == 4;

            NameOffset = 0;
            HashAlgorithmOffset = NameOffset + blobIndexSize;
            HashOffset = HashAlgorithmOffset + guidIndexSize;
            LanguageOffset = HashOffset + blobIndexSize;
            RowSize = LanguageOffset + guidIndexSize;
        }

        public DocumentNameBlobIndex GetName(DocumentIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new DocumentNameBlobIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigBlobIndex), blobHeap);
        }

        public GuidIndex GetHashAlgorithm(DocumentIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + HashAlgorithmOffset, isBigGuidIndex), guidHeap);
        }

        public BlobIndex GetHash(DocumentIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + HashOffset, isBigBlobIndex), blobHeap);
        }

        public GuidIndex GetLanguage(DocumentIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + LanguageOffset, isBigGuidIndex), guidHeap);
        }

        public long GetRowOffset(DocumentIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public DocumentRow this[DocumentIndex index] => GetRowSafe((int) index);

        protected override DocumentRow GetRow(int index) => new DocumentRow((DocumentIndex) index, this);
    }
}
