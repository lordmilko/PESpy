using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class MethodDefTable : Table<MethodDefRow>
    {
        internal readonly int RVAOffset;
        internal readonly int ImplFlagsOffset;
        internal readonly int FlagsOffset;
        internal readonly int NameOffset;
        internal readonly int SignatureOffset;
        internal readonly int ParamListOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;
        private readonly bool isBigParamIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal MethodDefTable(
            int numRows,
            int stringIndexSize,
            int blobIndexSize,
            int paramIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
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
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public ParamIndex GetParamList(MethodDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (ParamIndex) tableChunk.PeekEcmaIndex(rowOffset + ParamListOffset, isBigParamIndex);
        }

        internal void GetRange(TypeDefIndex typeDef, out int firstMethodRowId, out int lastMethodRowId)
        {
            firstMethodRowId = (int) CompressedModelHeap.TypeDefTable.GetMethodList(typeDef);

            if (firstMethodRowId == 0)
            {
                firstMethodRowId = 0;
                lastMethodRowId = 0;
            }
            else if (typeDef.RowId == CompressedModelHeap.TypeDefTable.Count)
            {
                lastMethodRowId = (CompressedModelHeap.MethodPtrTable?.Count > 0 ? CompressedModelHeap.MethodPtrTable.Count : CompressedModelHeap.MethodDefTable.Count) + 1;
            }
            else
            {
                lastMethodRowId = (int) CompressedModelHeap.TypeDefTable.GetMethodList((TypeDefIndex) (typeDef.RowId + 1));
            }
        }

        public CustomAttributeList GetCustomAttributes(MethodDefIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.MethodDef));

        public DeclSecurityAttributeList GetDeclSecurityAttributes(MethodDefIndex index) =>
            new DeclSecurityAttributeList(CompressedModelHeap, HasDeclSecurityTag.CreateIndex((int) index, TableKind.MethodDef));

        public long GetRowOffset(MethodDefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodDefRow this[MethodDefIndex index] => GetRowSafe((int) index);

        public MethodDefRow this[MethodDebugInformationIndex index] => GetRowSafe((int) index);

        protected override MethodDefRow GetRow(int index) => new MethodDefRow((MethodDefIndex) index, this);
    }
}
