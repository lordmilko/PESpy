using System.Diagnostics;
using PESpy.View;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.EventMapRow, this, ViewKind.Metadata_EventMapRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteSimpleIndex(nameof(Parent), (int) Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(EventList), (int) EventList, TableKind.Event);

            return s.ToArray();
        }
    }
}
