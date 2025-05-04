namespace PESpy.Ecma335
{
    public sealed class EventPtrTable : Table<EventPtrRow>
    {
        internal readonly int RowSize;

        private readonly int EventOffset;

        private readonly bool isBigEventIndex;

        private readonly MemoryChunk tableChunk;

        internal EventPtrTable(int numRows, int eventIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigEventIndex = eventIndexSize == 4;

            EventOffset = 0;
            RowSize = EventOffset + eventIndexSize;
        }

        public int GetEvent(EventPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekEcmaIndex(rowOffset + EventOffset, isBigEventIndex);
        }

        public int GetRowOffset(EventPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventPtrRow this[EventPtrIndex index] => this[(int) index];

        protected override EventPtrRow GetRow(int index) => new EventPtrRow((EventPtrIndex) index, this);
    }
}
