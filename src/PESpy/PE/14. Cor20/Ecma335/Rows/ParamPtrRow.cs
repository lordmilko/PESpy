using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Param = {Param}")]
    public readonly struct ParamPtrRow : IValue, IViewable
    {
        public ParamPtrIndex RowIndex { get; }

        public int Param => table.GetParam(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ParamPtrTable table;

        internal ParamPtrRow(ParamPtrIndex index, ParamPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ParamPtr Row", this, ViewKind.Metadata_ParamPtrRow);

            s.WriteValue(nameof(Param), Param);
        }
    }
}
