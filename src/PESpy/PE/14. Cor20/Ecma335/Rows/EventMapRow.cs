using System;
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

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Parent), table.ParentOffset, (int) Parent, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(EventList), table.EventListOffset, (int) EventList, TableKind.Event);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
