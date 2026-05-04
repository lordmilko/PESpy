using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class MethodSemanticsTable : Table<MethodSemanticsRow>
    {
        internal readonly int SemanticsOffset;
        internal readonly int MethodOffset;
        internal readonly int AssociationOffset;

        private readonly bool hasBigMethodDefIndex;
        private readonly bool hasBigHasSemanticsIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal MethodSemanticsTable(
            int numRows,
            int methodDefIndexSize,
            int hasSemanticsIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.28

            CompressedModelHeap = compressedModelHeap;

            hasBigMethodDefIndex = methodDefIndexSize == 4;
            hasBigHasSemanticsIndex = hasSemanticsIndexSize == 4;

            SemanticsOffset = 0;
            MethodOffset = SemanticsOffset + sizeof(ushort);
            AssociationOffset = MethodOffset + methodDefIndexSize;
            RowSize = AssociationOffset + hasSemanticsIndexSize;
        }

        public CorMethodSemanticsAttr GetSemantics(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorMethodSemanticsAttr) tableChunk.PeekUInt16(rowOffset + SemanticsOffset);
        }

        public MethodDefIndex GetMethod(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, hasBigMethodDefIndex);
        }

        public CodedIndex GetAssociation(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + AssociationOffset, hasBigHasSemanticsIndex, CodedIndexType.HasSemantics);
        }

        internal int FindSemanticMethods(CodedIndex index, ref ushort methodCount)
        {
            CompressedModelHeap.BinarySearchEcmaIndexRange(
                tableChunk,
                Count,
                RowSize,
                AssociationOffset,
                (uint) (int) index,
                hasBigHasSemanticsIndex,
                out var startRowNumber,
                out var endRowNumber
            );

            if (startRowNumber == -1)
            {
                methodCount = 0;
                return 0;
            }

            methodCount = (ushort) (endRowNumber - startRowNumber + 1);
            return startRowNumber + 1;
        }

        public long GetRowOffset(MethodSemanticsIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSemanticsRow this[MethodSemanticsIndex index] => GetRowSafe((int) index);

        protected override MethodSemanticsRow GetRow(int index) => new MethodSemanticsRow((MethodSemanticsIndex) index, this);
    }
}
