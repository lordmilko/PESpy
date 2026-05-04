using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {MethodRow}")]
    public readonly struct MethodPtrRow : IValue, IViewable
    {
        public MethodPtrIndex RowIndex { get; }

        public MethodDefIndex Method => table.GetMethod(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public MethodDefRow MethodRow => table.CompressedModelHeap.MethodDefTable[Method];

        private readonly MethodPtrTable table;

        internal MethodPtrRow(MethodPtrIndex index, MethodPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_MethodPtrRow, table.RowSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Method), table.MethodOffset, (int) Method);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
