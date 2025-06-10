using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EncMapTable : Table<EncMapRow>
    {
        internal readonly int RowSize;

        private readonly int TokenOffset;

        private readonly MemoryChunk tableChunk;

        internal EncMapTable(int numRows, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            TokenOffset = 0;
            RowSize = TokenOffset + sizeof(int);
        }

        public mdToken GetToken(EncMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + TokenOffset);
        }

        public int GetRowOffset(EncMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EncMapRow this[EncMapIndex index] => this[(int) index];

        protected override EncMapRow GetRow(int index) => new EncMapRow((EncMapIndex) index, this);
    }
}
