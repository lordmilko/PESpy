using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ImplMapTable : Table<ImplMapRow>
    {
        internal readonly int MappingFlagsOffset;
        internal readonly int MemberForwardedOffset;
        internal readonly int ImportNameOffset;
        internal readonly int ImportScopeOffset;

        private readonly bool isBigMemberForwardedIndex;
        private readonly bool isBigStringIndex;
        private readonly bool isBigModuleRefIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal ImplMapTable(int numRows, int memberForwardedIndexSize, int stringIndexSize, int moduleRefIndexSize, CompressedModelHeap compressedModelHeap, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.22

            CompressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;

            isBigMemberForwardedIndex = memberForwardedIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;
            isBigModuleRefIndex = moduleRefIndexSize == 4;

            MappingFlagsOffset = 0;
            MemberForwardedOffset = MappingFlagsOffset + sizeof(ushort);
            ImportNameOffset = MemberForwardedOffset + memberForwardedIndexSize;
            ImportScopeOffset = ImportNameOffset + stringIndexSize;
            RowSize = ImportScopeOffset + moduleRefIndexSize;
        }

        public CorPinvokeMap GetMappingFlags(ImplMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorPinvokeMap) tableChunk.PeekUInt16(rowOffset + MappingFlagsOffset);
        }

        public CodedIndex GetMemberForwarded(ImplMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + MemberForwardedOffset, isBigMemberForwardedIndex, CodedIndexType.MemberForwarded);
        }

        public StringIndex GetImportName(ImplMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + ImportNameOffset, isBigStringIndex), stringHeap);
        }

        public ModuleRefIndex GetImportScope(ImplMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (ModuleRefIndex) tableChunk.PeekEcmaIndex(rowOffset + ImportScopeOffset, isBigModuleRefIndex);
        }

        internal ImplMapIndex FindImplForMethod(MethodDefIndex index)
        {
            var codedIndex = MemberForwardedTag.CreateIndex(index.RowId, TableKind.MethodDef);

            var foundRowNumber = CompressedModelHeap.BinarySearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                MemberForwardedOffset,
                (uint) (int) codedIndex,
                isBigMemberForwardedIndex
            );

            return (ImplMapIndex) (foundRowNumber + 1);
        }

        public int GetRowOffset(ImplMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ImplMapRow this[ImplMapIndex index] => GetRow((int) index);

        protected override ImplMapRow GetRow(int index) => new ImplMapRow((ImplMapIndex) index, this);
    }
}
