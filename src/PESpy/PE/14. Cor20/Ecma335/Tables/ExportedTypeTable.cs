using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ExportedTypeTable : Table<ExportedTypeRow>
    {
        internal readonly int RowSize;

        private readonly int FlagsOffset;
        private readonly int TypeDefIdOffset;
        private readonly int TypeNameOffset;
        private readonly int TypeNamespaceOffset;
        private readonly int ImplementationOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigImplementationIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal ExportedTypeTable(int numRows, int stringIndexSize, int implementationIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigImplementationIndex = implementationIndexSize == 4;

            FlagsOffset = 0;
            TypeDefIdOffset = FlagsOffset + sizeof(uint);
            TypeNameOffset = TypeDefIdOffset + sizeof(uint);
            TypeNamespaceOffset = TypeNameOffset + stringIndexSize;
            ImplementationOffset = TypeNamespaceOffset + stringIndexSize;
            RowSize = ImplementationOffset + implementationIndexSize;
        }

        public CorTypeAttr GetFlags(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorTypeAttr) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public int GetTypeDefId(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + TypeDefIdOffset);
        }

        public StringIndex GetTypeName(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetTypeNamespace(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNamespaceOffset, isBigStringIndex), stringHeap);
        }

        public Index GetImplementation(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + ImplementationOffset, isBigImplementationIndex);
        }

        public int GetRowOffset(ExportedTypeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ExportedTypeRow this[ExportedTypeIndex index] => this[(int) index];

        protected override ExportedTypeRow GetRow(int index) => new ExportedTypeRow((ExportedTypeIndex) index, this);
    }
}
