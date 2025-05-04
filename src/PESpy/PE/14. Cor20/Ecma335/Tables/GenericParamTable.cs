using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class GenericParamTable : Table<GenericParamRow>
    {
        internal readonly int RowSize;

        private readonly int NumberOffset;
        private readonly int FlagsOffset;
        private readonly int OwnerOffset;
        private readonly int NameOffset;

        private readonly bool isBigTypeOrMethodDefIndex;
        private readonly bool isBigStringIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal GenericParamTable(int numRows, int typeOrMethodDefIndexSize, int stringIndexSize, Lazy<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
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

        public int GetOwner(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + OwnerOffset, isBigTypeOrMethodDefIndex);
        }

        public StringIndex GetName(GenericParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap.Value);
        }

        public int GetRowOffset(GenericParamIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public GenericParamRow this[GenericParamIndex index] => this[(int) index];

        protected override GenericParamRow GetRow(int index) => new GenericParamRow((GenericParamIndex) index, this);
    }
}
