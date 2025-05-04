using System.Diagnostics;
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

        public int Token => table.GetToken(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly EncMapTable table;

        internal EncMapRow(EncMapIndex index, EncMapTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EncMap Row", this, ViewKind.Metadata_EncMapRow);

            s.WriteValue(nameof(Token), Token);
        }
    }
}
