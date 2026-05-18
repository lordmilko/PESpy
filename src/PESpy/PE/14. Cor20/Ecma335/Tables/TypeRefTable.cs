using System;

namespace PESpy.Ecma335
{
    public sealed class TypeRefTable : Table<TypeRefRow>
    {
        internal readonly int ResolutionScopeOffset;
        internal readonly int TypeNameOffset;
        internal readonly int TypeNamespaceOffset;

        private readonly bool isBigResolutionScopeIndex;
        private readonly bool isBigStringIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal TypeRefTable(
            int numRows,
            int resolutionScopeIndexSize,
            int stringIndexSize,
            ModelHeap modelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.38

            ModelHeap = modelHeap;
            this.stringHeap = stringHeap;

            isBigResolutionScopeIndex = resolutionScopeIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;

            ResolutionScopeOffset = 0;
            TypeNameOffset = ResolutionScopeOffset + resolutionScopeIndexSize;
            TypeNamespaceOffset = TypeNameOffset + stringIndexSize;
            RowSize = TypeNamespaceOffset + stringIndexSize;
        }

        public CodedIndex GetResolutionScope(TypeRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ResolutionScopeOffset, isBigResolutionScopeIndex, CodedIndexType.ResolutionScope);
        }

        public StringIndex GetTypeName(TypeRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetTypeNamespace(TypeRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNamespaceOffset, isBigStringIndex), stringHeap);
        }

        public long GetRowOffset(TypeRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeRefRow this[TypeRefIndex index] => GetRowSafe((int) index);

        protected override TypeRefRow GetRow(int index) => new TypeRefRow((TypeRefIndex) index, this);
    }
}
