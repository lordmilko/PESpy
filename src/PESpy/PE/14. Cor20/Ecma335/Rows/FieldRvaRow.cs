using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, Field = {FieldRow}")]
    public readonly struct FieldRvaRow : IValue, IViewable
    {
        public FieldRvaIndex RowIndex { get; }

        public int RVA => table.GetRVA(RowIndex);

        public FieldIndex Field => table.GetField(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public FieldRow FieldRow => table.ModelHeap.FieldTable[Field];

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
            writer.NewStruct(this, ViewKind.Metadata_FieldRvaRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), table.RVAOffset, RVA);
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(Field), table.FieldOffset, (int) Field, TableKind.Field);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
