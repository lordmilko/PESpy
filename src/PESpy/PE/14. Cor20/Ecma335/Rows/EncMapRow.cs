using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Token = {TokenRow}")]
    public readonly struct EncMapRow : IValue, IViewable
    {
        public EncMapIndex RowIndex { get; }

        public mdToken Token => table.GetToken(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object TokenRow => table.ModelHeap.GetRow(Token);

        private readonly EncMapTable table;

        internal EncMapRow(EncMapIndex index, EncMapTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_EncMapRow, table.RowSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Token), table.TokenOffset, Token);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
