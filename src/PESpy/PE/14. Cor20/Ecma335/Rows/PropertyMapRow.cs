using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, PropertyList = {PropertyList}")]
    public readonly struct PropertyMapRow : IValue, IViewable
    {
        public PropertyMapIndex RowIndex { get; }

        public TypeDefIndex Parent => table.GetParent(RowIndex);

        public PropertyIndex PropertyList => table.GetPropertyList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly PropertyMapTable table;

        internal PropertyMapRow(PropertyMapIndex index, PropertyMapTable table)
        {
            //II.22.35

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PropertyMapRow, this, ViewKind.Metadata_PropertyMapRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteSimpleIndex(nameof(Parent), (int) Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(PropertyList), (int) PropertyList, TableKind.Property);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
