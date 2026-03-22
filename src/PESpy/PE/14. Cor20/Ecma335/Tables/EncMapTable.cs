using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EncMapTable : Table<EncMapRow>
    {
        internal readonly int TokenOffset;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal EncMapTable(
            int numRows,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            TokenOffset = 0;
            RowSize = TokenOffset + sizeof(int);
        }

        public mdToken GetToken(EncMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + TokenOffset);
        }

        public int GetRowOffset(EncMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EncMapRow this[EncMapIndex index] => GetRow((int) index);

        protected override EncMapRow GetRow(int index) => new EncMapRow((EncMapIndex) index, this);
    }
}
