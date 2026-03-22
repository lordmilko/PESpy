using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Param = {ParamRow}")]
    public readonly struct ParamPtrRow : IValue, IViewable
    {
        public ParamPtrIndex RowIndex { get; }

        public ParamIndex Param => table.GetParam(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public ParamRow ParamRow => table.CompressedModelHeap.ParamTable[Param];

        private readonly ParamPtrTable table;

        internal ParamPtrRow(ParamPtrIndex index, ParamPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ParamPtrRow, this, ViewKind.Metadata_ParamPtrRow, table.RowSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Param), table.ParamOffset, (int) Param);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
