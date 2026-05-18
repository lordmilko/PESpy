using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class GenericParamTable : Table<GenericParamRow>
    {
        internal readonly int NumberOffset;
        internal readonly int FlagsOffset;
        internal readonly int OwnerOffset;
        internal readonly int NameOffset;

        private readonly bool isBigTypeOrMethodDefIndex;
        private readonly bool isBigStringIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal GenericParamTable(int numRows, int typeOrMethodDefIndexSize, int stringIndexSize, ModelHeap modelHeap, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.20

            ModelHeap = modelHeap;
            this.stringHeap = stringHeap;

            isBigTypeOrMethodDefIndex = typeOrMethodDefIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;

            NumberOffset = 0;
            FlagsOffset = NumberOffset + sizeof(ushort);
            OwnerOffset = FlagsOffset + sizeof(ushort);
            NameOffset = OwnerOffset + typeOrMethodDefIndexSize;
            RowSize = NameOffset + stringIndexSize;
        }

        public short GetNumber(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + NumberOffset);
        }

        public CorGenericParamAttr GetFlags(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorGenericParamAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public CodedIndex GetOwner(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + OwnerOffset, isBigTypeOrMethodDefIndex, CodedIndexType.TypeOrMethodDef);
        }

        public StringIndex GetName(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        internal GenericParamList FindGenericParameters(CodedIndex index)
        {
            ModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                OwnerOffset,
                (uint) (int) index,
                isBigTypeOrMethodDefIndex,
                out var startRowNumber,
                out var endRowNumber
            );

            int startRid;
            ushort genericParamCount;

            if (startRowNumber == -1)
            {
                genericParamCount = 0;
                startRid = 0;
            }
            else
            {
                genericParamCount = (ushort) (endRowNumber - startRowNumber + 1);
                startRid = startRowNumber + 1;
            }

            return new GenericParamList(startRid, genericParamCount, ModelHeap);
        }

        public CustomAttributeList GetCustomAttributes(GenericParamIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.GenericParam));

        public long GetRowOffset(GenericParamIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public GenericParamRow this[GenericParamIndex index] => GetRowSafe((int) index);

        protected override GenericParamRow GetRow(int index) => new GenericParamRow((GenericParamIndex) index, this);
    }
}
