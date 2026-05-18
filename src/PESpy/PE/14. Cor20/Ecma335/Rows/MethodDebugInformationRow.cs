using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Document = {DocumentRow}, SequencePoints = {SequencePoints}")]
    public readonly struct MethodDebugInformationRow : IValue, IViewable
    {
        public MethodDebugInformationIndex RowIndex { get; }

        public DocumentIndex Document => table.GetDocument(RowIndex);

        public BlobIndex SequencePoints => table.GetSequencePoints(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public DocumentRow? DocumentRow
        {
            get
            {
                var document = Document;

                if (document.IsNil)
                    return null;

                return table.ModelHeap.DocumentTable[document];
            }
        }

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
            writer.NewStruct(this, ViewKind.PortablePdb_MethodDebugInformationRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Document), table.DocumentOffset, (int) Document);
                    break;

                case 1:
                    structWriter.WriteBlobHeapIndex(nameof(SequencePoints), table.SequencePointsOffset, SequencePoints);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
