using System;
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

        int IViewable.NumChildren => 2;

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
