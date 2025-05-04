using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}")]
    public readonly struct MethodPtrRow : IValue, IViewable
    {
        public MethodPtrIndex RowIndex { get; }

        public int Method => table.GetMethod(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodPtrTable table;

        internal MethodPtrRow(MethodPtrIndex index, MethodPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodPtr Row", this, ViewKind.Metadata_MethodPtrRow);

            s.WriteValue(nameof(Method), Method);
        }
    }
}
