using System;

namespace PESpy.Ecma335
{
    public sealed class LocalConstantTable : Table<LocalConstantRow>
    {
        internal readonly int NameOffset;
        internal readonly int SignatureOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal LocalConstantTable(
            int numRows,
            int stringIndexSize,
            int blobIndexSize,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
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

        internal void GetRange(LocalScopeIndex localScope, out int firstConstantRowId, out int lastConstantRowId)
        {
            firstConstantRowId = (int) CompressedModelHeap.LocalScopeTable.GetConstantList(localScope);

            if (firstConstantRowId == 0)
            {
                firstConstantRowId = 0;
                lastConstantRowId = 0;
            }
            else if (localScope.RowId == CompressedModelHeap.LocalScopeTable.Count)
            {
                lastConstantRowId = Count + 1;
            }
            else
            {
                lastConstantRowId = (int) CompressedModelHeap.LocalScopeTable.GetVariableList((LocalScopeIndex) (localScope.RowId + 1));
            }
        }

        public long GetRowOffset(LocalConstantIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public LocalConstantRow this[LocalConstantIndex index] => GetRowSafe((int) index);

        protected override LocalConstantRow GetRow(int index) => new LocalConstantRow((LocalConstantIndex) index, this);
    }
}
