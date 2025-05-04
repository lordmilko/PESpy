using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Type = {Type}, Value = {Value}")]
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public CustomAttributeIndex RowIndex { get; }

        public int Parent => table.GetParent(RowIndex);

        public int Type => table.GetType(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomAttributeTable table;

        internal CustomAttributeRow(CustomAttributeIndex index, CustomAttributeTable table)
        {
            //II.22.10

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("CustomAttribute Row", this, ViewKind.Metadata_CustomAttributeRow);

            s.WriteHasCustomAttributeIndex(nameof(Parent), Parent);
            s.WriteCustomAttributeTypeIndex(nameof(Type), Type);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
