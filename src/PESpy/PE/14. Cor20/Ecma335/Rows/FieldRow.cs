using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, Name = {Name.ToString(),nq}, Signature = {Signature}")]
    public readonly struct FieldRow : IValue, IViewable
    {
        public FieldIndex RowIndex { get; }

        public CorFieldAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldTable table;

        internal FieldRow(FieldIndex index, FieldTable table)
        {
            //II.22.15

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FieldRow, this, ViewKind.Metadata_FieldRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);

            return s.ToArray();
        }
    }
}
