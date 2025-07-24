using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Token), Token);
            s.WriteValue(nameof(FuncCode), FuncCode, sizeof(int));

            return s.ToArray();
        }
    }
}
