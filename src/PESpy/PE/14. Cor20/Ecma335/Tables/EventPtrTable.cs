namespace PESpy.Ecma335
{
    public sealed class EventPtrTable : Table<EventPtrRow>
    {
        internal readonly int EventOffset;

        private readonly bool isBigEventIndex;

        internal readonly ModelHeap ModelHeap;

        internal EventPtrTable(
            int numRows,
            int eventIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

            isBigEventIndex = eventIndexSize == 4;

            EventOffset = 0;
            RowSize = EventOffset + eventIndexSize;
        }

        public EventIndex GetEvent(EventPtrIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (EventIndex) tableChunk.PeekEcmaIndex(rowOffset + EventOffset, isBigEventIndex);
        }

        public long GetRowOffset(EventPtrIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventPtrRow this[EventPtrIndex index] => GetRowSafe((int) index);

        protected override EventPtrRow GetRow(int index) => new EventPtrRow((EventPtrIndex) index, this);
    }
}
