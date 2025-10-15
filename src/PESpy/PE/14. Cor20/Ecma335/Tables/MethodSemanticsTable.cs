using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class MethodSemanticsTable : Table<MethodSemanticsRow>
    {
        internal readonly int RowSize;

        internal readonly int SemanticsOffset;
        internal readonly int MethodOffset;
        internal readonly int AssociationOffset;

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

        public MethodDefIndex GetMethod(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, hasBigMethodDefIndex);
        }

        public Index GetAssociation(MethodSemanticsIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + AssociationOffset, hasBigHasSemanticsIndex);
        }

        public int GetRowOffset(MethodSemanticsIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSemanticsRow this[MethodSemanticsIndex index] => this[(int) index];

        protected override MethodSemanticsRow GetRow(int index) => new MethodSemanticsRow((MethodSemanticsIndex) index, this);
    }
}
