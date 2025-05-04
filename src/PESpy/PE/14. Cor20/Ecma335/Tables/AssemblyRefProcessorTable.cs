namespace PESpy.Ecma335
{
    public sealed class AssemblyRefProcessorTable : Table<AssemblyRefProcessorRow>
    {
        internal readonly int RowSize;

        private readonly int ProcessorOffset;
        private readonly int AssemblyRefOffset;

        private readonly bool isBigAssemblyRefIndex;

        private readonly MemoryChunk tableChunk;

        internal AssemblyRefProcessorTable(int numRows, int assemblyRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetAssemblyRef(AssemblyRefProcessorIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + AssemblyRefOffset, isBigAssemblyRefIndex);
        }

        public int GetRowOffset(AssemblyRefProcessorIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefProcessorRow this[AssemblyRefProcessorIndex index] => this[(int) index];

        protected override AssemblyRefProcessorRow GetRow(int index) => new AssemblyRefProcessorRow((AssemblyRefProcessorIndex) index, this);
    }
}
