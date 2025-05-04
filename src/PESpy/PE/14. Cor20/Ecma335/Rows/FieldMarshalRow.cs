using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, NativeType = {NativeType}")]
    public readonly struct FieldMarshalRow : IValue, IViewable
    {
        public FieldMarshalIndex RowIndex { get; }

        public int Parent => table.GetParent(RowIndex);

        public BlobIndex NativeType => table.GetNativeType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldMarshalTable table;

        internal FieldMarshalRow(FieldMarshalIndex index, FieldMarshalTable table)
        {
            //II.22.17

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldMarshal Row", this, ViewKind.Metadata_FieldMarshalRow);

            s.WriteHasFieldMarshalIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(NativeType), NativeType);
        }
    }
}
