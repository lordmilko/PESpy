namespace PESpy.Ecma335
{
    public sealed class ParamPtrTable : Table<ParamPtrRow>
    {
        internal readonly int ParamOffset;

        private readonly bool isBigParamIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal ParamPtrTable(
            int numRows,
            int paramIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

            isBigParamIndex = paramIndexSize == 4;

            ParamOffset = 0;
            RowSize = ParamOffset + paramIndexSize;
        }

        public ParamIndex GetParam(ParamPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (ParamIndex) tableChunk.PeekEcmaIndex(rowOffset + ParamOffset, isBigParamIndex);
        }

        public int GetRowOffset(ParamPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ParamPtrRow this[ParamPtrIndex index] => GetRow((int) index);

        protected override ParamPtrRow GetRow(int index) => new ParamPtrRow((ParamPtrIndex) index, this);
    }
}
