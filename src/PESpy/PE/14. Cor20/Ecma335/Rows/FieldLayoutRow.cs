using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("FieldOffset = {FieldOffset}, Field = {FieldRow}")]
    public readonly struct FieldLayoutRow : IValue, IViewable
    {
        public FieldLayoutIndex RowIndex { get; }

        public int FieldOffset => table.GetFieldOffset(RowIndex);

        public FieldIndex Field => table.GetField(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public FieldRow FieldRow => table.CompressedModelHeap.FieldTable[Field];

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
            writer.NewStruct(this, ViewKind.Metadata_FieldLayoutRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(FieldOffset), table.FieldOffsetOffset, FieldOffset);
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
