using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class TypeDefTable : Table<TypeDefRow>
    {
        internal readonly int RowSize;

        internal readonly int FlagsOffset;
        internal readonly int TypeNameOffset;
        internal readonly int TypeNamespaceOffset;
        internal readonly int ExtendsOffset;
        internal readonly int FieldListOffset;
        internal readonly int MethodListOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigTypeDefOrRefIndex;
        private readonly bool isBigFieldIndex;
        private readonly bool isBigMethodIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal TypeDefTable(
            int numRows,
            int stringIndexSize,
            int typeDefOrRefIndexSize,
            int fieldIndexSize,
            int methodIndexSize,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(numRows)
        {
            //II.22.37

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
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetTypeNamespace(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNamespaceOffset, isBigStringIndex), stringHeap);
        }

        public CodedIndex GetExtends(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ExtendsOffset, isBigTypeDefOrRefIndex, CodedIndexType.TypeDefOrRef);
        }

        public FieldIndex GetFieldList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldListOffset, isBigFieldIndex);
        }

        public MethodDefIndex GetMethodList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodListOffset, isBigMethodIndex);
        }

        internal TypeDefRow FindTypeContainingMethod(int methodRowId, int numberOfMethods)
        {
            var row = CompressedModelHeap.BinarySearchEcmaIndexList(
                tableChunk,
                Count,
                RowSize,
                MethodListOffset,
                (uint) methodRowId,
                isBigMethodIndex
            ) + 1;

            if (row == 0)
                return default;

            if (row > Count)
            {
                if (methodRowId <= numberOfMethods)
                    return this[Count]; //It's the last type

                return default;
            }

            var methodList = GetMethodList((TypeDefIndex) row);

            if (methodList.RowId == methodRowId)
            {
                //Not 100% clear on why dotnet/runtime does this. You could have multiple types referencing the method?

                while (row < Count)
                {
                    var nextRow = row + 1;

                    var nextMethodList = GetMethodList((TypeDefIndex) nextRow);

                    if (nextMethodList.RowId == methodRowId)
                        row = nextRow;
                    else
                        break;
                }

                return this[row];
            }

            return this[Count];
        }

        public int GetRowOffset(TypeDefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeDefRow this[TypeDefIndex index] => this[(int) index];

        protected override TypeDefRow GetRow(int index) => new TypeDefRow((TypeDefIndex) index, this);
    }
}
