using System;

namespace PESpy.Ecma335
{
    public sealed class ModuleTable : Table<ModuleRow>
    {
        internal readonly int GenerationOffset;
        internal readonly int NameOffset;
        internal readonly int MvidOffset;
        internal readonly int EncIdOffset;
        internal readonly int EncBaseIdOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigGuidIndex;

        private readonly CompressedModelHeap compressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<GuidHeap?> guidHeap;

        internal ModuleTable(
            int numRows,
            int stringIndexSize,
            int guidIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<GuidHeap?> guidHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            this.compressedModelHeap = compressedModelHeap;
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
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public GuidIndex GetMvid(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + MvidOffset, isBigGuidIndex), guidHeap);
        }

        public GuidIndex GetEncId(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + EncIdOffset, isBigGuidIndex), guidHeap);
        }

        public GuidIndex GetEncBaseId(ModuleIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new GuidIndex(tableChunk.PeekEcmaIndex(rowOffset + EncBaseIdOffset, isBigGuidIndex), guidHeap);
        }

        public CustomAttributeList GetCustomAttributes(ModuleIndex index) =>
            new CustomAttributeList(compressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Module));

        public int GetRowOffset(ModuleIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ModuleRow this[ModuleIndex index] => GetRow((int) index);

        protected override ModuleRow GetRow(int index) => new ModuleRow((ModuleIndex) index, this);
    }
}
