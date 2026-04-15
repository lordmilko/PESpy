using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class DeclSecurityTable : Table<DeclSecurityRow>
    {
        internal readonly int ActionOffset;
        internal readonly int ParentOffset;
        internal readonly int PermissionSetOffset;

        private readonly bool isBigHasDeclSecurityIndex;
        private readonly bool isBigBlobIndexSize;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal DeclSecurityTable(
            int numRows,
            int hasDeclSecurityIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.11

            CompressedModelHeap = compressedModelHeap;
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

        internal void GetRange(CodedIndex index, out int firstRowId, out int lastRowId)
        {
            CompressedModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) (int) index,
                isBigHasDeclSecurityIndex,
                out var startRowNumber,
                out var endRowNumber
            );

            if (startRowNumber == -1)
            {
                firstRowId = 0;
                lastRowId = 0;
            }
            else
            {
                firstRowId = startRowNumber + 1;
                lastRowId = endRowNumber + 2; //+1 gets us the actual last row and we want +2 to be +1 past it
            }
        }

        public int GetRowOffset(DeclSecurityIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public DeclSecurityRow this[DeclSecurityIndex index] => GetRowSafe((int) index);

        protected override DeclSecurityRow GetRow(int index) => new DeclSecurityRow((DeclSecurityIndex) index, this);
    }
}
