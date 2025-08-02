using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, Field = {Field}")]
    public readonly struct FieldRvaRow : IValue, IViewable
    {
        public FieldRvaIndex RowIndex { get; }

        public int RVA => table.GetRVA(RowIndex);

        public FieldIndex Field => table.GetField(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldRvaTable table;

        internal FieldRvaRow(FieldRvaIndex index, FieldRvaTable table)
        {
            //II.22.18

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FieldRvaRow, this, ViewKind.Metadata_FieldRvaRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(RVA), RVA);
            s.WriteSimpleIndex(nameof(Field), (int) Field, TableKind.Field);

            return s.ToArray();
        }
    }
}
