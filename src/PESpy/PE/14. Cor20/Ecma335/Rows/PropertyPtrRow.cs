using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Property = {Property}")]
    public readonly struct PropertyPtrRow : IValue, IViewable
    {
        public PropertyPtrIndex RowIndex { get; }

        public PropertyIndex Property => table.GetProperty(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly PropertyPtrTable table;

        internal PropertyPtrRow(PropertyPtrIndex index, PropertyPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PropertyPtrRow, this, ViewKind.Metadata_PropertyPtrRow, table.RowSize);

        int IViewable.NumChildren => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Property), table.PropertyOffset, (int) Property);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
