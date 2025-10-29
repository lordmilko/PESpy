using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class DeclSecurityTable : Table<DeclSecurityRow>
    {
        internal readonly int RowSize;

        internal readonly int ActionOffset;
        internal readonly int ParentOffset;
        internal readonly int PermissionSetOffset;

        private readonly bool isBigHasDeclSecurityIndex;
        private readonly bool isBigBlobIndexSize;

        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal DeclSecurityTable(int numRows, int hasDeclSecurityIndexSize, int blobIndexSize, Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            //II.22.11

            this.tableChunk = tableChunk;
            this.blobHeap = blobHeap;

            isBigHasDeclSecurityIndex = hasDeclSecurityIndexSize == 4;
            isBigBlobIndexSize = blobIndexSize == 4;

            ActionOffset = 0;
            ParentOffset = ActionOffset + sizeof(ushort);
            PermissionSetOffset = ParentOffset + hasDeclSecurityIndexSize;
            RowSize = PermissionSetOffset + blobIndexSize;
        }

        public CorDeclSecurity GetAction(DeclSecurityIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorDeclSecurity) tableChunk.PeekUInt16(rowOffset + ActionOffset);
        }

        public CodedIndex GetParent(DeclSecurityIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ParentOffset, isBigHasDeclSecurityIndex, CodedIndexType.HasDeclSecurity);
        }

        public BlobIndex GetPermissionSet(DeclSecurityIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + PermissionSetOffset, isBigBlobIndexSize), blobHeap);
        }

        public int GetRowOffset(DeclSecurityIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public DeclSecurityRow this[DeclSecurityIndex index] => this[(int) index];

        protected override DeclSecurityRow GetRow(int index) => new DeclSecurityRow((DeclSecurityIndex) index, this);
    }
}
