namespace PESpy.Ecma335
{
    public sealed class GenericParamConstraintTable : Table<GenericParamConstraintRow>
    {
        internal readonly int OwnerOffset;
        internal readonly int ConstraintOffset;

        private readonly bool isBigGenericParamIndex;
        private readonly bool isBigTypeDefOrRefIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal GenericParamConstraintTable(int numRows, int genericParamIndexSize, int typeDefOrRefIndexSize, CompressedModelHeap compressedModelHeap, in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.21

            CompressedModelHeap = compressedModelHeap;

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

        public CodedIndex GetConstraint(GenericParamConstraintIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ConstraintOffset, isBigTypeDefOrRefIndex, CodedIndexType.TypeDefOrRef);
        }

        public GenericParamConstraintList FindConstraintsForGenericParam(GenericParamIndex index)
        {
            CompressedModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                OwnerOffset,
                (uint) index.RowId,
                isBigGenericParamIndex,
                out var startRowNumber,
                out var endRowNumber
            );

            if (startRowNumber == -1)
                return default;

            return new GenericParamConstraintList(
                firstRowId: startRowNumber + 1,
                count: (ushort) (endRowNumber - startRowNumber + 1),
                CompressedModelHeap
            );
        }

        public CustomAttributeList GetCustomAttributes(GenericParamConstraintIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.GenericParamConstraint));

        public long GetRowOffset(GenericParamConstraintIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public GenericParamConstraintRow this[GenericParamConstraintIndex index] => GetRowSafe((int) index);

        protected override GenericParamConstraintRow GetRow(int index) => new GenericParamConstraintRow((GenericParamConstraintIndex) index, this);
    }
}
