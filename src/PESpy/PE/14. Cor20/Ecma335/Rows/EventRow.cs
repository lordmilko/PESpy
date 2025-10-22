using System;
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

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(EventFlags), table.EventFlagsOffset, EventFlags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteTypeDefOrRefIndex(nameof(EventType), table.EventTypeOffset, (int) EventType);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
