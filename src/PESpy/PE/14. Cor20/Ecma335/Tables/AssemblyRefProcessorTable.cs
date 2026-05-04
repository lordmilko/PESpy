namespace PESpy.Ecma335
{
    public sealed class AssemblyRefProcessorTable : Table<AssemblyRefProcessorRow>
    {
        internal readonly int ProcessorOffset;
        internal readonly int AssemblyRefOffset;

        private readonly bool isBigAssemblyRefIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal AssemblyRefProcessorTable(
            int numRows,
            int assemblyRefIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigAssemblyRefIndex = assemblyRefIndexSize == 4;

            ProcessorOffset = 0;
            AssemblyRefOffset = ProcessorOffset + sizeof(int);
            RowSize = AssemblyRefOffset + assemblyRefIndexSize;
        }

        public int GetProcessor(AssemblyRefProcessorIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ProcessorOffset);
        }

        public AssemblyRefIndex GetAssemblyRef(AssemblyRefProcessorIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (AssemblyRefIndex) tableChunk.PeekEcmaIndex(rowOffset + AssemblyRefOffset, isBigAssemblyRefIndex);
        }

        public long GetRowOffset(AssemblyRefProcessorIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefProcessorRow this[AssemblyRefProcessorIndex index] => GetRowSafe((int) index);

        protected override AssemblyRefProcessorRow GetRow(int index) => new AssemblyRefProcessorRow((AssemblyRefProcessorIndex) index, this);
    }
}
