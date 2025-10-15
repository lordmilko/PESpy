using System;

namespace PESpy.Ecma335
{
    public sealed class LocalVariableTable : Table<LocalVariableRow>
    {
        internal readonly int RowSize;

        internal readonly int AttributesOffset;
        internal readonly int IndexOffset;
        internal readonly int NameOffset;

        private readonly bool isBigStringIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal LocalVariableTable(int numRows, int stringIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;

            AttributesOffset = 0;
            IndexOffset = AttributesOffset + sizeof(short);
            NameOffset = IndexOffset + sizeof(short);
            RowSize = NameOffset + stringIndexSize;
        }

        public LocalVariableAttributes GetAttributes(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (LocalVariableAttributes) tableChunk.PeekUInt16(rowOffset + AttributesOffset);
        }

        public uint GetIndex(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekUInt16(rowOffset + IndexOffset);
        }

        public StringIndex GetName(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public int GetRowOffset(LocalVariableIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public LocalVariableRow this[LocalVariableIndex index] => this[(int) index];

        protected override LocalVariableRow GetRow(int index) => new LocalVariableRow((LocalVariableIndex) index, this);
    }
}
