using System;

namespace PESpy.Ecma335
{
    public sealed class ModuleRefTable : Table<ModuleRefRow>
    {
        internal readonly int NameOffset;

        private readonly bool isBigStringIndex;

        private readonly CompressedModelHeap compressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal ModuleRefTable(
            int numRows,
            int stringIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            this.compressedModelHeap = compressedModelHeap;
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

        public CustomAttributeList GetCustomAttributes(ModuleRefIndex index) =>
            new CustomAttributeList(compressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.ModuleRef));

        public long GetRowOffset(ModuleRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ModuleRefRow this[ModuleRefIndex index] => GetRowSafe((int) index);

        protected override ModuleRefRow GetRow(int index) => new ModuleRefRow((ModuleRefIndex) index, this);
    }
}
