using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class FieldTable : Table<FieldRow>
    {
        internal readonly int RowSize;

        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal FieldTable(int numRows, int stringIndexSize, int blobIndexSize, Func<StringHeap?> stringHeap, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            FlagsOffset = 0;
            NameOffset = FlagsOffset + sizeof(ushort);
            SignatureOffset = NameOffset + stringIndexSize;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public CorFieldAttr GetFlags(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorFieldAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(FieldIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(FieldIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FieldRow this[FieldIndex index] => this[(int) index];

        protected override FieldRow GetRow(int index) => new FieldRow((FieldIndex) index, this);
    }
}
