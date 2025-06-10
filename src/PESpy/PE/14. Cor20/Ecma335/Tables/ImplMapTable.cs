using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ImplMapTable : Table<ImplMapRow>
    {
        internal readonly int RowSize;

        private readonly int MappingFlagsOffset;
        private readonly int MemberForwardedOffset;
        private readonly int ImportNameOffset;
        private readonly int ImportScopeOffset;

        private readonly bool isBigMemberForwardedIndex;
        private readonly bool isBigStringIndex;
        private readonly bool isBigModuleRefIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal ImplMapTable(int numRows, int memberForwardedIndexSize, int stringIndexSize, int moduleRefIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
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

        public Index GetMemberForwarded(ImplMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + MemberForwardedOffset, isBigMemberForwardedIndex);
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

        public int GetRowOffset(ImplMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ImplMapRow this[ImplMapIndex index] => this[(int) index];

        protected override ImplMapRow GetRow(int index) => new ImplMapRow((ImplMapIndex) index, this);
    }
}
