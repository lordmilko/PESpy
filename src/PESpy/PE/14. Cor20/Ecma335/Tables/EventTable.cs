using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EventTable : Table<EventRow>
    {
        internal readonly int RowSize;

        internal readonly int EventFlagsOffset;
        internal readonly int NameOffset;
        internal readonly int EventTypeOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigTypeDefOrRefIndexSize;

        private readonly Func<StringHeap?> stringHeap;
        private readonly MemoryChunk tableChunk;

        internal EventTable(int numRows, int stringIndexSize, int typeDefOrRefIndexSize, Func<StringHeap?> stringHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigTypeDefOrRefIndexSize = typeDefOrRefIndexSize == 4;

            EventFlagsOffset = 0;
            NameOffset = EventFlagsOffset + sizeof(short);
            EventTypeOffset = NameOffset + stringIndexSize;
            RowSize = EventTypeOffset + typeDefOrRefIndexSize;
        }

        public CorEventAttr GetEventFlags(EventIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorEventAttr) tableChunk.PeekUInt16(rowOffset + EventFlagsOffset);
        }

        public StringIndex GetName(EventIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public Index GetEventType(EventIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (Index) tableChunk.PeekEcmaIndex(rowOffset + EventTypeOffset, isBigTypeDefOrRefIndexSize);
        }

        public int GetRowOffset(EventIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventRow this[EventIndex index] => this[(int) index];

        protected override EventRow GetRow(int index) => new EventRow((EventIndex) index, this);
    }
}
