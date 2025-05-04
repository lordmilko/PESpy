using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("FieldOffset = {FieldOffset}, Field = {Field}")]
    public readonly struct FieldLayoutRow : IValue, IViewable
    {
        public FieldLayoutIndex RowIndex { get; }

        public int FieldOffset => table.GetFieldOffset(RowIndex);

        public int Field => table.GetField(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FieldLayoutTable table;

        internal FieldLayoutRow(FieldLayoutIndex index, FieldLayoutTable table)
        {
            //II.22.16

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldLayout Row", this, ViewKind.Metadata_FieldLayoutRow);

            s.WriteValue(nameof(FieldOffset), FieldOffset);
            s.WriteSimpleIndex(nameof(Field), Field, TableKind.Field);
        }
    }
}
