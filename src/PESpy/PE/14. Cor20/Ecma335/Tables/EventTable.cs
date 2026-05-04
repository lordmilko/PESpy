using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class EventTable : Table<EventRow>
    {
        internal readonly int EventFlagsOffset;
        internal readonly int NameOffset;
        internal readonly int EventTypeOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigTypeDefOrRefIndexSize;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal EventTable(
            int numRows,
            int stringIndexSize,
            int typeDefOrRefIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.13

            CompressedModelHeap = compressedModelHeap;
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

        public CodedIndex GetEventType(EventIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + EventTypeOffset, isBigTypeDefOrRefIndexSize, CodedIndexType.TypeDefOrRef);
        }

        public CustomAttributeList GetCustomAttributes(EventIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Event));

        public long GetRowOffset(EventIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public EventRow this[EventIndex index] => GetRowSafe((int) index);

        protected override EventRow GetRow(int index) => new EventRow((EventIndex) index, this);
    }
}
