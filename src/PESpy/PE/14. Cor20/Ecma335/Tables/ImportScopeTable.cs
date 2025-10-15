using System;

namespace PESpy.Ecma335
{
    public sealed class ImportScopeTable : Table<ImportScopeRow>
    {
        internal readonly int RowSize;

        internal readonly int ParentOffset;
        internal readonly int ImportsOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigImportScopeIndex;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal ImportScopeTable(
            int numRows,
            int blobIndexSize,
            int importScopeIndexSize,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;
            isBigImportScopeIndex = importScopeIndexSize == 4;

            ParentOffset = 0;
            ImportsOffset = ParentOffset + importScopeIndexSize;
            RowSize = ImportsOffset + blobIndexSize;
        }

        public ImportScopeIndex GetParent(ImportScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (ImportScopeIndex) tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigImportScopeIndex);
        }

        public BlobIndex GetImports(ImportScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + ImportsOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(ImportScopeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ImportScopeRow this[ImportScopeIndex index] => this[(int) index];

        protected override ImportScopeRow GetRow(int index) => new ImportScopeRow((ImportScopeIndex) index, this);
    }
}
