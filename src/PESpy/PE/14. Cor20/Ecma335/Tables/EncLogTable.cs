using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EncLogTable : Table<EncLogRow>
    {
        internal readonly int RowSize;

        private readonly int TokenOffset;
        private readonly int FuncCodeOffset;

        private readonly MemoryChunk tableChunk;

        internal EncLogTable(int numRows, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            TokenOffset = 0;
            FuncCodeOffset = TokenOffset + sizeof(int);
            RowSize = FuncCodeOffset + sizeof(int);
        }

        public mdToken GetToken(EncLogIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + TokenOffset);
        }

        public EditAndContinueOperation GetFuncCode(EncLogIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (EditAndContinueOperation) tableChunk.PeekUInt32(rowOffset + FuncCodeOffset);
        }

        public int GetRowOffset(EncLogIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EncLogRow this[EncLogIndex index] => this[(int) index];

        protected override EncLogRow GetRow(int index) => new EncLogRow((EncLogIndex) index, this);
    }
}
