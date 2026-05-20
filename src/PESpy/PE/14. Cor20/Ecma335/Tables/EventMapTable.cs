namespace PESpy.Ecma335
{
    public sealed class EventMapTable : Table<EventMapRow>
    {
        internal readonly int ParentOffset;
        internal readonly int EventListOffset;

        private readonly bool isBigTypeDefIndex;
        private readonly bool isBigEventIndex;

        internal readonly ModelHeap ModelHeap;

        internal EventMapTable(
            int numRows,
            int typeDefIndexSize,
            int eventIndexSize,
            ModelHeap modelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;

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

        internal TypeDefRow? FindTypeContainingEvent(int eventRowId, int numberOfEvents)
        {
            var numberOfRows = Count;

            var row = ModelHeap.BinarySearchEcmaIndexList(
                tableChunk,
                numberOfRows,
                RowSize,
                EventListOffset,
                (uint) eventRowId,
                isBigEventIndex
            ) + 1;

            if (row == 0)
                return default;

            if (row > numberOfRows)
            {
                if (eventRowId <= numberOfEvents)
                    return ModelHeap.TypeDefTable[GetParent((EventMapIndex) numberOfRows)];

                return default;
            }

            return ModelHeap.TypeDefTable[GetParent((EventMapIndex) row)];
        }

        internal void GetRange(TypeDefIndex typeDef, out int firstEventRowId, out int lastEventRowId)
        {
            var eventMapRowId = FindEventMapRowIdFor(typeDef);

            if (eventMapRowId == 0)
            {
                firstEventRowId = 0;
                lastEventRowId = 0;
                return;
            }

            firstEventRowId = (int) GetEventList((EventMapIndex) eventMapRowId);

            if (eventMapRowId == Count)
            {
                lastEventRowId = (ModelHeap.EventPtrTable?.Count > 0 ? ModelHeap.EventPtrTable.Count : ModelHeap.EventTable.Count) + 1;
            }
            else
                lastEventRowId = (int) GetEventList((EventMapIndex) (eventMapRowId + 1));
        }

        private int FindEventMapRowIdFor(TypeDefIndex typeDef)
        {
            //Apparently these tables aren't sorted so we have to linear scan
            var rowNumber = ModelHeap.LinearSearchEcmaIndex(
                tableChunk,
                Count,
                RowSize,
                ParentOffset,
                (uint) typeDef.RowId,
                isBigTypeDefIndex
            );

            return rowNumber + 1;
        }

        public long GetRowOffset(EventMapIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventMapRow this[EventMapIndex index] => GetRowSafe((int) index);

        protected override EventMapRow GetRow(int index) => new EventMapRow((EventMapIndex) index, this);
    }
}
