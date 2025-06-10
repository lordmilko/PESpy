using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("PropertyPtr Row", this, ViewKind.Metadata_PropertyPtrRow);

            s.WriteValue(nameof(Property), (int) Property);
        }
    }
}
