using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("EventFlags = {EventFlags}, Name = {Name.ToString(),nq}, EventType = {EventType}")]
    public readonly struct EventRow : IValue, IViewable
    {
        public EventIndex RowIndex { get; }

        public CorEventAttr EventFlags => table.GetEventFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public int EventType => table.GetEventType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EventTable table;

        internal EventRow(EventIndex index, EventTable table)
        {
            //II.22.13

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Event Row", this, ViewKind.Metadata_EventRow);

            s.WriteValue(nameof(EventFlags), EventFlags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteTypeDefOrRefIndex(nameof(EventType), EventType);
        }
    }
}
