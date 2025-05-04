using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, PropertyList = {PropertyList}")]
    public readonly struct PropertyMapRow : IValue, IViewable
    {
        public PropertyMapIndex RowIndex { get; }

        public int Parent => table.GetParent(RowIndex);

        public int PropertyList => table.GetPropertyList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly PropertyMapTable table;

        internal PropertyMapRow(PropertyMapIndex index, PropertyMapTable table)
        {
            //II.22.35

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("PropertyMap Row", this, ViewKind.Metadata_PropertyMapRow);

            s.WriteSimpleIndex(nameof(Parent), Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(PropertyList), PropertyList, TableKind.Property);
        }
    }
}
