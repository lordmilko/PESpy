using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class MethodDefTable : Table<MethodDefRow>
    {
        internal readonly int RowSize;

        private readonly int RVAOffset;
        private readonly int ImplFlagsOffset;
        private readonly int FlagsOffset;
        private readonly int NameOffset;
        private readonly int SignatureOffset;
        private readonly int ParamListOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;
        private readonly bool isBigParamIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal MethodDefTable(int numRows, int stringIndexSize, int blobIndexSize, int paramIndexSize, Lazy<StringHeap?> stringHeap, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;
            isBigParamIndex = paramIndexSize == 4;

            RVAOffset = 0;
            ImplFlagsOffset = RVAOffset + sizeof(int);
            FlagsOffset = ImplFlagsOffset + sizeof(ushort);
            NameOffset = FlagsOffset + sizeof(ushort);
            SignatureOffset = NameOffset + stringIndexSize;
            ParamListOffset = SignatureOffset + blobIndexSize;
            RowSize = ParamListOffset + paramIndexSize;
        }

        public int GetRVA(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + RVAOffset);
        }

        public CorMethodImpl GetImplFlags(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorMethodImpl) tableChunk.PeekUInt16(rowOffset + ImplFlagsOffset);
        }

        public CorMethodAttr GetFlags(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorMethodAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap.Value);
        }

        public BlobIndex GetSignature(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetParamList(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ParamListOffset, isBigParamIndex);
        }

        public int GetRowOffset(MethodDefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodDefRow this[MethodDefIndex index] => this[(int) index];

        protected override MethodDefRow GetRow(int index) => new MethodDefRow((MethodDefIndex) index, this);
    }
}
