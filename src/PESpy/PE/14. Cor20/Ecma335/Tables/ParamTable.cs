using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ParamTable : Table<ParamRow>
    {
        internal readonly int RowSize;

        private readonly bool isBigStringIndex;

        internal readonly int FlagsOffset;
        internal readonly int SequenceOffset;
        internal readonly int NameOffset;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal ParamTable(int numRows, int stringIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;

            FlagsOffset = 0;
            SequenceOffset = FlagsOffset + sizeof(ushort);
            NameOffset = SequenceOffset + sizeof(ushort);
            RowSize = NameOffset + stringIndexSize;
        }

        public CorParamAttr GetFlags(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorParamAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public short GetSequence(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + SequenceOffset);
        }

        public StringIndex GetName(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public int GetRowOffset(ParamIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ParamRow this[ParamIndex index] => this[(int) index];

        protected override ParamRow GetRow(int index) => new ParamRow((ParamIndex) index, this);
    }
}
