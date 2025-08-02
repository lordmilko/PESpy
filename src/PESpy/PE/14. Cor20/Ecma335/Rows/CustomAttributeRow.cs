using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Type = {Type}, Value = {Value}")]
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public CustomAttributeIndex RowIndex { get; }

        public Index Parent => table.GetParent(RowIndex);

        public Index Type => table.GetType(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomAttributeTable table;

        internal CustomAttributeRow(CustomAttributeIndex index, CustomAttributeTable table)
        {
            //II.22.10

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomAttributeRow, this, ViewKind.Metadata_CustomAttributeRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteHasCustomAttributeIndex(nameof(Parent), (int) Parent);
            s.WriteCustomAttributeTypeIndex(nameof(Type), (int) Type);
            s.WriteBlobHeapIndex(nameof(Value), Value);

            return s.ToArray();
        }
    }
}
