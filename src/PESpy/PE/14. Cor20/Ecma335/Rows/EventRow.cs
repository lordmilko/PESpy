using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("EventFlags = {EventFlags}, Name = {Name.ToString(),nq}, EventType = {EventType}")]
    public readonly struct EventRow : IValue, IViewable
    {
        public EventIndex RowIndex { get; }

        public CorEventAttr EventFlags => table.GetEventFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public Index EventType => table.GetEventType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EventTable table;

        internal EventRow(EventIndex index, EventTable table)
        {
            //II.22.13

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.EventRow, this, ViewKind.Metadata_EventRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(EventFlags), EventFlags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteTypeDefOrRefIndex(nameof(EventType), (int) EventType);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
