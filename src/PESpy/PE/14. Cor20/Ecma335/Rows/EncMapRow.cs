using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Token = {Token}")]
    public readonly struct EncMapRow : IValue, IViewable
    {
        public EncMapIndex RowIndex { get; }

        public mdToken Token => table.GetToken(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

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
            writer.NewStruct(Strings.EncMapRow, this, ViewKind.Metadata_EncMapRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Token), Token);

            return s.ToArray();
        }
    }
}
