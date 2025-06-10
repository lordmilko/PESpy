namespace PESpy.Ecma335
{
    public sealed class ParamPtrTable : Table<ParamPtrRow>
    {
        internal readonly int RowSize;

        private readonly int ParamOffset;

        private readonly bool isBigParamIndex;

        private readonly MemoryChunk tableChunk;

        internal ParamPtrTable(int numRows, int paramIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public ParamPtrRow this[ParamPtrIndex index] => this[(int) index];

        protected override ParamPtrRow GetRow(int index) => new ParamPtrRow((ParamPtrIndex) index, this);
    }
}
