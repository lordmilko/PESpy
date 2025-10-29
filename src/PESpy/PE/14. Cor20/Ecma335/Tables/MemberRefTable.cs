using System;

namespace PESpy.Ecma335
{
    public sealed class MemberRefTable : Table<MemberRefRow>
    {
        internal readonly int RowSize;

        internal readonly int ClassOffset;
        internal readonly int NameOffset;
        internal readonly int SignatureOffset;

        private readonly bool isBigMemberRefParentIndex;
        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal MemberRefTable(int numRows, int memberRefParentIndexSize, int stringIndexSize, int blobIndexSize, Func<StringHeap?> stringHeap, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            //II.22.25

            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigMemberRefParentIndex = memberRefParentIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            ClassOffset = 0;
            NameOffset = ClassOffset + memberRefParentIndexSize;
            SignatureOffset = NameOffset + stringIndexSize;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public CodedIndex GetClass(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ClassOffset, isBigMemberRefParentIndex, CodedIndexType.MemberRefParent);
        }

        public StringIndex GetName(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public int GetRowOffset(MemberRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MemberRefRow this[MemberRefIndex index] => this[(int) index];

        protected override MemberRefRow GetRow(int index) => new MemberRefRow((MemberRefIndex) index, this);
    }
}
