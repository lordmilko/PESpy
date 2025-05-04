namespace PESpy.Ecma335
{
    public sealed class StateMachineMethodTable : Table<StateMachineMethodRow>
    {
        internal readonly int RowSize;

        private readonly int MoveNextMethodOffset;
        private readonly int KickoffMethodOffset;

        private readonly MemoryChunk tableChunk;

        internal StateMachineMethodTable(int numRows, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            MoveNextMethodOffset = 0;
            KickoffMethodOffset = MoveNextMethodOffset + sizeof(int);
            RowSize = KickoffMethodOffset + sizeof(int);
        }

        public int GetMoveNextMethod(StateMachineMethodIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + MoveNextMethodOffset);
        }

        public int GetKickoffMethod(StateMachineMethodIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + KickoffMethodOffset);
        }

        public int GetRowOffset(StateMachineMethodIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public StateMachineMethodRow this[StateMachineMethodIndex index] => this[(int) index];

        protected override StateMachineMethodRow GetRow(int index) => new StateMachineMethodRow((StateMachineMethodIndex) index, this);
    }
}
