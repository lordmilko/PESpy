using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Token = {Token}, FuncCode = {FuncCode}")]
    public readonly struct EncLogRow : IValue, IViewable
    {
        public EncLogIndex RowIndex { get; }

        public mdToken Token => table.GetToken(RowIndex);

        public EditAndContinueOperation FuncCode => table.GetFuncCode(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EncLogTable table;

        internal EncLogRow(EncLogIndex index, EncLogTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.EncLogRow, this, ViewKind.Metadata_EncLogRow, table.RowSize);

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Token), table.TokenOffset, Token);
                    break;

                case 1:
                    structWriter.WriteField(nameof(FuncCode), table.FuncCodeOffset, FuncCode, sizeof(int));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
