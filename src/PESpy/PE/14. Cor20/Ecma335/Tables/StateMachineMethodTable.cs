namespace PESpy.Ecma335
{
    public sealed class StateMachineMethodTable : Table<StateMachineMethodRow>
    {
        internal readonly int RowSize;

        private readonly int MoveNextMethodOffset;
        private readonly int KickoffMethodOffset;

        private readonly bool isBigMethodIndex;

        private readonly MemoryChunk tableChunk;

        internal StateMachineMethodTable(int numRows, int methodIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigMethodIndex = methodIndexSize == 4;

            MoveNextMethodOffset = 0;
            KickoffMethodOffset = MoveNextMethodOffset + sizeof(int);
            RowSize = KickoffMethodOffset + sizeof(int);
        }

        public MethodDefIndex GetMoveNextMethod(StateMachineMethodIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MoveNextMethodOffset, isBigMethodIndex);
        }

        public MethodDefIndex GetKickoffMethod(StateMachineMethodIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + KickoffMethodOffset, isBigMethodIndex);
        }

        public int GetRowOffset(StateMachineMethodIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public StateMachineMethodRow this[StateMachineMethodIndex index] => this[(int) index];

        protected override StateMachineMethodRow GetRow(int index) => new StateMachineMethodRow((StateMachineMethodIndex) index, this);
    }
}
