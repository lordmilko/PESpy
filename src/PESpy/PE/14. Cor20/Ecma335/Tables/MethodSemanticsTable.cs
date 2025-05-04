using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class MethodSemanticsTable : Table<MethodSemanticsRow>
    {
        internal readonly int RowSize;

        private readonly int SemanticsOffset;
        private readonly int MethodOffset;
        private readonly int AssociationOffset;

        private readonly bool hasBigMethodDefIndex;
        private readonly bool hasBigHasSemanticsIndex;

        private readonly MemoryChunk tableChunk;

        internal MethodSemanticsTable(int numRows, int methodDefIndexSize, int hasSemanticsIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

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

        public int GetMethod(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, hasBigMethodDefIndex);
        }

        public int GetAssociation(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + AssociationOffset, hasBigHasSemanticsIndex);
        }

        public int GetRowOffset(MethodSemanticsIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSemanticsRow this[MethodSemanticsIndex index] => this[(int) index];

        protected override MethodSemanticsRow GetRow(int index) => new MethodSemanticsRow((MethodSemanticsIndex) index, this);
    }
}
