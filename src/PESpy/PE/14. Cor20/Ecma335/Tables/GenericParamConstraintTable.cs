namespace PESpy.Ecma335
{
    public sealed class GenericParamConstraintTable : Table<GenericParamConstraintRow>
    {
        internal readonly int RowSize;

        private readonly int OwnerOffset;
        private readonly int ConstraintOffset;

        private readonly bool isBigGenericParamIndex;
        private readonly bool isBigTypeDefOrRefIndex;

        private readonly MemoryChunk tableChunk;

        internal GenericParamConstraintTable(int numRows, int genericParamIndexSize, int typeDefOrRefIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigGenericParamIndex = genericParamIndexSize == 4;
            isBigTypeDefOrRefIndex = typeDefOrRefIndexSize == 4;

            OwnerOffset = 0;
            ConstraintOffset = OwnerOffset + genericParamIndexSize;
            RowSize = ConstraintOffset + typeDefOrRefIndexSize;
        }

        public GenericParamIndex GetOwner(GenericParamConstraintIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (GenericParamIndex) tableChunk.PeekEcmaIndex(rowOffset + OwnerOffset, isBigGenericParamIndex);
        }

        public Index GetConstraint(GenericParamConstraintIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + ConstraintOffset, isBigTypeDefOrRefIndex);
        }

        public int GetRowOffset(GenericParamConstraintIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public GenericParamConstraintRow this[GenericParamConstraintIndex index] => this[(int) index];

        protected override GenericParamConstraintRow GetRow(int index) => new GenericParamConstraintRow((GenericParamConstraintIndex) index, this);
    }
}
