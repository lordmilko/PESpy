using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ExportedTypeTable : Table<ExportedTypeRow>
    {
        internal readonly int FlagsOffset;
        internal readonly int TypeDefIdOffset;
        internal readonly int TypeNameOffset;
        internal readonly int TypeNamespaceOffset;
        internal readonly int ImplementationOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigImplementationIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal ExportedTypeTable(
            int numRows,
            int stringIndexSize,
            int implementationIndexSize,
            ModelHeap modelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.14

            ModelHeap = modelHeap;
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

        public CodedIndex GetImplementation(ExportedTypeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ImplementationOffset, isBigImplementationIndex, CodedIndexType.Implementation);
        }

        public CustomAttributeList GetCustomAttributes(ExportedTypeIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.ExportedType));

        public long GetRowOffset(ExportedTypeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ExportedTypeRow this[ExportedTypeIndex index] => GetRowSafe((int) index);

        protected override ExportedTypeRow GetRow(int index) => new ExportedTypeRow((ExportedTypeIndex) index, this);
    }
}
