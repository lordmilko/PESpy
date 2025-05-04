using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, Field = {Field}")]
    public readonly struct FieldRvaRow : IValue, IViewable
    {
        public FieldRvaIndex RowIndex { get; }

        public int RVA => table.GetRVA(RowIndex);

        public int Field => table.GetField(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldRvaTable table;

        internal FieldRvaRow(FieldRvaIndex index, FieldRvaTable table)
        {
            //II.22.18

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldRva Row", this, ViewKind.Metadata_FieldRvaRow);

            s.WriteValue(nameof(RVA), RVA);
            s.WriteSimpleIndex(nameof(Field), Field, TableKind.Field);
        }
    }
}
