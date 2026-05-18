using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EncLogTable : Table<EncLogRow>
    {
        internal readonly int TokenOffset;
        internal readonly int FuncCodeOffset;

        internal readonly ModelHeap ModelHeap;

        internal EncLogTable(
            int numRows,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

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

        public long GetRowOffset(EncLogIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EncLogRow this[EncLogIndex index] => GetRowSafe((int) index);

        protected override EncLogRow GetRow(int index) => new EncLogRow((EncLogIndex) index, this);
    }
}
