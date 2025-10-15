using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class GenericParamTable : Table<GenericParamRow>
    {
        internal readonly int RowSize;

        internal readonly int NumberOffset;
        internal readonly int FlagsOffset;
        internal readonly int OwnerOffset;
        internal readonly int NameOffset;

        private readonly bool isBigTypeOrMethodDefIndex;
        private readonly bool isBigStringIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal GenericParamTable(int numRows, int typeOrMethodDefIndexSize, int stringIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
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

        public Index GetOwner(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + OwnerOffset, isBigTypeOrMethodDefIndex);
        }

        public StringIndex GetName(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public int GetRowOffset(GenericParamIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public GenericParamRow this[GenericParamIndex index] => this[(int) index];

        protected override GenericParamRow GetRow(int index) => new GenericParamRow((GenericParamIndex) index, this);
    }
}
