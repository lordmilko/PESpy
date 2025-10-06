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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Property), (int) Property);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
