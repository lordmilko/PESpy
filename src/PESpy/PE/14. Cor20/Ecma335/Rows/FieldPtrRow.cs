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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("FieldPtr Row", this, ViewKind.Metadata_FieldPtrRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Field), (int) Field);

            return s.ToArray();
        }
    }
}
