using System;
using System.Diagnostics;
using PESpy.View;

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

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Event), table.EventOffset, (int) Event);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
