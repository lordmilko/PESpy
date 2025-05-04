using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class TypeDefTable : Table<TypeDefRow>
    {
        internal readonly int RowSize;

        private readonly int FlagsOffset;
        private readonly int TypeNameOffset;
        private readonly int TypeNamespaceOffset;
        private readonly int ExtendsOffset;
        private readonly int FieldListOffset;
        private readonly int MethodListOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigTypeDefOrRefIndex;
        private readonly bool isBigFieldIndex;
        private readonly bool isBigMethodIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal TypeDefTable(
            int numRows,
            int stringIndexSize,
            int typeDefOrRefIndexSize,
            int fieldIndexSize,
            int methodIndexSize,
            Lazy<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigTypeDefOrRefIndex = typeDefOrRefIndexSize == 4;
            isBigFieldIndex = fieldIndexSize == 4;
            isBigMethodIndex = methodIndexSize == 4;

            FlagsOffset = 0;
            TypeNameOffset = FlagsOffset + sizeof(int);
            TypeNamespaceOffset = TypeNameOffset + stringIndexSize;
            ExtendsOffset = TypeNamespaceOffset + stringIndexSize;
            FieldListOffset = ExtendsOffset + typeDefOrRefIndexSize;
            MethodListOffset = FieldListOffset + fieldIndexSize;
            RowSize = MethodListOffset + methodIndexSize;
        }

        public CorTypeAttr GetFlags(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorTypeAttr) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public StringIndex GetTypeName(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNameOffset, isBigStringIndex), stringHeap.Value);
        }

        public StringIndex GetTypeNamespace(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNamespaceOffset, isBigStringIndex), stringHeap.Value);
        }

        public int GetExtends(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + ExtendsOffset, isBigTypeDefOrRefIndex);
        }

        public int GetFieldList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + FieldListOffset, isBigFieldIndex);
        }

        public int GetMethodList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodListOffset, isBigMethodIndex);
        }

        public int GetRowOffset(TypeDefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeDefRow this[TypeDefIndex index] => this[(int) index];

        protected override TypeDefRow GetRow(int index) => new TypeDefRow((TypeDefIndex) index, this);
    }
}
