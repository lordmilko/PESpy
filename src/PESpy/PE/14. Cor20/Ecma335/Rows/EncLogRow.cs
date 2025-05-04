using System.Diagnostics;
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

        public int Token => table.GetToken(RowIndex);

        public EditAndContinueOperation FuncCode => table.GetFuncCode(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EncLogTable table;

        internal EncLogRow(EncLogIndex index, EncLogTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EncLog Row", this, ViewKind.Metadata_EncLogRow);

            s.WriteValue(nameof(Token), Token);
            s.WriteValue(nameof(FuncCode), FuncCode, sizeof(int));
        }
    }
}
