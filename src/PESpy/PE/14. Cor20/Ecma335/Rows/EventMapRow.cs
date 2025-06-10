using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, EventList = {EventList}")]
    public readonly struct EventMapRow : IValue, IViewable
    {
        public EventMapIndex RowIndex { get; }

        public TypeDefIndex Parent => table.GetParent(RowIndex);

        public EventIndex EventList => table.GetEventList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EventMapTable table;

        internal EventMapRow(EventMapIndex index, EventMapTable table)
        {
            //II.22.12

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EventMap Row", this, ViewKind.Metadata_EventMapRow);

            s.WriteSimpleIndex(nameof(Parent), (int) Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(EventList), (int) EventList, TableKind.Event);
        }
    }
}
