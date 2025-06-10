using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Field = {Field}")]
    public readonly struct FieldPtrRow : IValue, IViewable
    {
        public FieldPtrIndex RowIndex { get; }

        public FieldIndex Field => table.GetField(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldPtrTable table;

        internal FieldPtrRow(FieldPtrIndex index, FieldPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldPtr Row", this, ViewKind.Metadata_FieldPtrRow);

            s.WriteValue(nameof(Field), (int) Field);
        }
    }
}
