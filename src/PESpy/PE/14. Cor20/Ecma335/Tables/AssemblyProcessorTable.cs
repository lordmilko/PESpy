namespace PESpy.Ecma335
{
    public sealed class AssemblyProcessorTable : Table<AssemblyProcessorRow>
    {
        internal readonly int ProcessorOffset;

        internal AssemblyProcessorTable(
            int numRows,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ProcessorOffset = 0;
            RowSize = ProcessorOffset + sizeof(int);
        }

        internal int GetProcessor(AssemblyProcessorIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ProcessorOffset);
        }

        public int GetRowOffset(AssemblyProcessorIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyProcessorRow this[AssemblyProcessorIndex index] => GetRowSafe((int) index);

        protected override AssemblyProcessorRow GetRow(int index) => new AssemblyProcessorRow((AssemblyProcessorIndex) index, this);
    }
}
