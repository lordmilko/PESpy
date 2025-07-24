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

        public Index Parent => table.GetParent(RowIndex);

        public BlobIndex NativeType => table.GetNativeType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldMarshalTable table;

        internal FieldMarshalRow(FieldMarshalIndex index, FieldMarshalTable table)
        {
            //II.22.17

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FieldMarshalRow, this, ViewKind.Metadata_FieldMarshalRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteHasFieldMarshalIndex(nameof(Parent), (int) Parent);
            s.WriteBlobHeapIndex(nameof(NativeType), NativeType);

            return s.ToArray();
        }
    }
}
