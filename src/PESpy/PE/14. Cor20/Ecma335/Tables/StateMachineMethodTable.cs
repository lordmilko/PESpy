namespace PESpy.Ecma335
{
    public sealed class StateMachineMethodTable : Table<StateMachineMethodRow>
    {
        internal readonly int MoveNextMethodOffset;
        internal readonly int KickoffMethodOffset;

        private readonly bool isBigMethodIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;

        internal StateMachineMethodTable(
            int numRows,
            int methodIndexSize,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;

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

        public long GetRowOffset(StateMachineMethodIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public StateMachineMethodRow this[StateMachineMethodIndex index] => GetRowSafe((int) index);

        protected override StateMachineMethodRow GetRow(int index) => new StateMachineMethodRow((StateMachineMethodIndex) index, this);
    }
}
