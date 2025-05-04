using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class DeclSecurityTable : Table<DeclSecurityRow>
    {
        internal readonly int RowSize;

        private readonly int ActionOffset;
        private readonly int ParentOffset;
        private readonly int PermissionSetOffset;

        private readonly bool isBigHasDeclSecurityIndex;
        private readonly bool isBigBlobIndexSize;

        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal DeclSecurityTable(int numRows, int hasDeclSecurityIndexSize, int blobIndexSize, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
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

        public int GetParent(DeclSecurityIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigHasDeclSecurityIndex);
        }

        public BlobIndex GetPermissionSet(DeclSecurityIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + PermissionSetOffset, isBigBlobIndexSize), blobHeap.Value);
        }

        public int GetRowOffset(DeclSecurityIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public DeclSecurityRow this[DeclSecurityIndex index] => this[(int) index];

        protected override DeclSecurityRow GetRow(int index) => new DeclSecurityRow((DeclSecurityIndex) index, this);
    }
}
