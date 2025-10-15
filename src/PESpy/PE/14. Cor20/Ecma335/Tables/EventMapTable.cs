namespace PESpy.Ecma335
{
    public sealed class EventMapTable : Table<EventMapRow>
    {
        internal readonly int RowSize;

        internal readonly int ParentOffset;
        internal readonly int EventListOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigEventIndex;

        private readonly MemoryChunk tableChunk;

        internal EventMapTable(int numRows, int typeDefIndexSize, int eventIndexSize, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigTypeDefIndex = typeDefIndexSize == 4;
            isBigEventIndex = eventIndexSize == 4;

            ParentOffset = 0;
            EventListOffset = ParentOffset + typeDefIndexSize;
            RowSize = EventListOffset + eventIndexSize;
        }

        public TypeDefIndex GetParent(EventMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (TypeDefIndex) tableChunk.PeekEcmaIndex(rowOffset + ParentOffset, isBigTypeDefIndex);
        }

        public EventIndex GetEventList(EventMapIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (EventIndex) tableChunk.PeekEcmaIndex(rowOffset + EventListOffset, isBigEventIndex);
        }

        public int GetRowOffset(EventMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventMapRow this[EventMapIndex index] => this[(int) index];

        protected override EventMapRow GetRow(int index) => new EventMapRow((EventMapIndex) index, this);
    }
}
