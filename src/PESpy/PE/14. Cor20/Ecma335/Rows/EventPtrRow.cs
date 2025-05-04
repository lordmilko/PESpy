using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Event = {Event}")]
    public readonly struct EventPtrRow : IValue, IViewable
    {
        public EventPtrIndex RowIndex { get; }

        public int Event => table.GetEvent(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EventPtrTable table;

        internal EventPtrRow(EventPtrIndex index, EventPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EventPtr Row", this, ViewKind.Metadata_EventPtrRow);

            s.WriteValue(nameof(Event), Event);
        }
    }
}
