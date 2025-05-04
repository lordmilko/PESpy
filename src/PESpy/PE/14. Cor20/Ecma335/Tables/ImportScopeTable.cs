using System;

namespace PESpy.Ecma335
{
    public sealed class ImportScopeTable : Table<ImportScopeRow>
    {
        internal readonly int RowSize;

        private readonly int ParentOffset;
        private readonly int ImportsOffset;

        private readonly bool isBigBlobIndex;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal ImportScopeTable(int numRows, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;

            ParentOffset = 0;
            ImportsOffset = ParentOffset + sizeof(int);
            RowSize = ImportsOffset + blobIndexSize;
        }

        public int GetParent(ImportScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ParentOffset);
        }

        public BlobIndex GetImports(ImportScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ImportsOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(ImportScopeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ImportScopeRow this[ImportScopeIndex index] => this[(int) index];

        protected override ImportScopeRow GetRow(int index) => new ImportScopeRow((ImportScopeIndex) index, this);
    }
}
