using System;

namespace PESpy.Ecma335
{
    public sealed class ModuleTable : Table<ModuleRow>
    {
        internal readonly int RowSize;

        private readonly int GenerationOffset;
        private readonly int NameOffset;
        private readonly int MvidOffset;
        private readonly int EncIdOffset;
        private readonly int EncBaseIdOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigGuidIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly Lazy<GuidHeap?> guidHeap;
        private readonly MemoryChunk tableChunk;

        internal ModuleTable(int numRows, int stringIndexSize, int guidIndexSize, Lazy<StringHeap?> stringHeap, Lazy<GuidHeap?> guidHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.guidHeap = guidHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigGuidIndex = guidIndexSize == 4;

            GenerationOffset = 0;
            NameOffset = GenerationOffset + sizeof(ushort);
            MvidOffset = NameOffset + stringIndexSize;
            EncIdOffset = MvidOffset + guidIndexSize;
            EncBaseIdOffset = EncIdOffset + guidIndexSize;
            RowSize = EncBaseIdOffset + guidIndexSize;
        }

        public short GetGeneration(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + GenerationOffset);
        }

        public StringIndex GetName(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap.Value);
        }

        public GuidIndex GetMvid(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + MvidOffset, isBigGuidIndex), guidHeap.Value);
        }

        public GuidIndex GetEncId(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + EncIdOffset, isBigGuidIndex), guidHeap.Value);
        }

        public GuidIndex GetEncBaseId(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + EncBaseIdOffset, isBigGuidIndex), guidHeap.Value);
        }

        public int GetRowOffset(ModuleIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ModuleRow this[ModuleIndex index] => this[(int) index];

        protected override ModuleRow GetRow(int index) => new ModuleRow((ModuleIndex) index, this);
    }
}
