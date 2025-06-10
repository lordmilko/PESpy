using System;

namespace PESpy.Ecma335
{
    public sealed class ModuleRefTable : Table<ModuleRefRow>
    {
        internal readonly int RowSize;

        private readonly int NameOffset;

        private readonly bool isBigStringIndex;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal ModuleRefTable(int numRows, int stringIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            this.isBigStringIndex = stringIndexSize == 4;

            NameOffset = 0;
            RowSize = NameOffset + stringIndexSize;
        }

        public StringIndex GetName(ModuleRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public int GetRowOffset(ModuleRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ModuleRefRow this[ModuleRefIndex index] => this[(int) index];

        protected override ModuleRefRow GetRow(int index) => new ModuleRefRow((ModuleRefIndex) index, this);
    }
}
