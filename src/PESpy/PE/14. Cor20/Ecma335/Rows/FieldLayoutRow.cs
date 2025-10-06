using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("FieldOffset = {FieldOffset}, Field = {Field}")]
    public readonly struct FieldLayoutRow : IValue, IViewable
    {
        public FieldLayoutIndex RowIndex { get; }

        public int FieldOffset => table.GetFieldOffset(RowIndex);

        public FieldIndex Field => table.GetField(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldLayoutTable table;

        internal FieldLayoutRow(FieldLayoutIndex index, FieldLayoutTable table)
        {
            //II.22.16

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FieldLayoutRow, this, ViewKind.Metadata_FieldLayoutRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(FieldOffset), FieldOffset);
            s.WriteSimpleIndex(nameof(Field), (int) Field, TableKind.Field);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
