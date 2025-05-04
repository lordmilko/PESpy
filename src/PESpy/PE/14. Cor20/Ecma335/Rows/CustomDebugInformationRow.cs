using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Kind = {Kind}, Value = {Value}")]
    public readonly struct CustomDebugInformationRow : IValue, IViewable
    {
        public CustomDebugInformationIndex RowIndex { get; }

        public int Parent => table.GetParent(RowIndex);

        public GuidIndex Kind => table.GetKind(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomDebugInformationTable table;

        internal CustomDebugInformationRow(CustomDebugInformationIndex index, CustomDebugInformationTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("CustomDebugInformation Row", this, ViewKind.PortablePdb_CustomDebugInformationRow);

            s.WriteHasCustomDebugInformationIndex(nameof(Parent), Parent);
            s.WriteGuidHeapIndex(nameof(Kind), Kind);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
