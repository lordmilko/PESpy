using System;

namespace PESpy.Ecma335
{
    public sealed class TypeRefTable : Table<TypeRefRow>
    {
        internal readonly int RowSize;

        internal readonly int ResolutionScopeOffset;
        internal readonly int TypeNameOffset;
        internal readonly int TypeNamespaceOffset;

        private readonly bool isBigResolutionScopeIndex;
        private readonly bool isBigStringIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal TypeRefTable(int numRows, int resolutionScopeIndexSize, int stringIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigResolutionScopeIndex = resolutionScopeIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;

            ResolutionScopeOffset = 0;
            TypeNameOffset = ResolutionScopeOffset + resolutionScopeIndexSize;
            TypeNamespaceOffset = TypeNameOffset + stringIndexSize;
            RowSize = TypeNamespaceOffset + stringIndexSize;
        }

        public Index GetResolutionScope(TypeRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + ResolutionScopeOffset, isBigResolutionScopeIndex);
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

        public int GetRowOffset(TypeRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeRefRow this[TypeRefIndex index] => this[(int) index];

        protected override TypeRefRow GetRow(int index) => new TypeRefRow((TypeRefIndex) index, this);
    }
}
