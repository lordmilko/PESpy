using System;
using System.Diagnostics;
using PESpy.View;

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
            writer.NewStruct(Strings.FieldPtrRow, this, ViewKind.Metadata_FieldPtrRow, table.RowSize);

        int IViewable.NumChildren => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Field), table.FieldOffset, (int) Field);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
