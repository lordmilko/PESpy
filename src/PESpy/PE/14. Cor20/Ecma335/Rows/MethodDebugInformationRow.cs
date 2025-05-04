using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Document = {Document}, SequencePoints = {SequencePoints}")]
    public readonly struct MethodDebugInformationRow : IValue, IViewable
    {
        public MethodDebugInformationIndex RowIndex { get; }

        public int Document => table.GetDocument(RowIndex);

        public BlobIndex SequencePoints => table.GetSequencePoints(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodDebugInformationTable table;

        internal MethodDebugInformationRow(MethodDebugInformationIndex index, MethodDebugInformationTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#methoddebuginformation-table-0x31

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodDebugInformation Row", this, ViewKind.PortablePdb_MethodDebugInformationRow);

            s.WriteValue(nameof(Document), Document);
            s.WriteBlobHeapIndex(nameof(SequencePoints), SequencePoints);
        }
    }
}
