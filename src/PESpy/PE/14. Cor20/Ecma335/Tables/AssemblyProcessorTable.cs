namespace PESpy.Ecma335
{
    public sealed class AssemblyProcessorTable : Table<AssemblyProcessorRow>
    {
        internal readonly int RowSize;

        internal readonly int ProcessorOffset;

        private readonly MemoryChunk tableChunk;

        internal AssemblyProcessorTable(int numRows, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            ProcessorOffset = 0;
            RowSize = ProcessorOffset + sizeof(int);
        }

        internal int GetProcessor(AssemblyProcessorIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ProcessorOffset);
        }

        public int GetRowOffset(AssemblyProcessorIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyProcessorRow this[AssemblyProcessorIndex index] => this[(int) index];

        protected override AssemblyProcessorRow GetRow(int index) => new AssemblyProcessorRow((AssemblyProcessorIndex) index, this);
    }
}
