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

        public EventIndex Event => table.GetEvent(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EventPtrTable table;

        internal EventPtrRow(EventPtrIndex index, EventPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.EventPtrRow, this, ViewKind.Metadata_EventPtrRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Event), (int) Event);

            return s.ToArray();
        }
    }
}
