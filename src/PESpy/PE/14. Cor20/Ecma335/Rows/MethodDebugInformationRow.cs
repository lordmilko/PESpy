using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Document = {Document}, SequencePoints = {SequencePoints}")]
    public readonly struct MethodDebugInformationRow : IValue, IViewable
    {
        public MethodDebugInformationIndex RowIndex { get; }

        public DocumentIndex Document => table.GetDocument(RowIndex);

        public BlobIndex SequencePoints => table.GetSequencePoints(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodDebugInformationTable table;

        internal MethodDebugInformationRow(MethodDebugInformationIndex index, MethodDebugInformationTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#methoddebuginformation-table-0x31

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodDebugInformationRow, this, ViewKind.PortablePdb_MethodDebugInformationRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Document), (int) Document);
            s.WriteBlobHeapIndex(nameof(SequencePoints), SequencePoints);

            return s.ToArray();
        }
    }
}
