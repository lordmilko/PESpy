using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Signature = {Signature}")]
    public readonly struct StandAloneSigRow : IValue, IViewable
    {
        public StandAloneSigIndex RowIndex { get; }

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly StandAloneSigTable table;

        internal StandAloneSigRow(StandAloneSigIndex index, StandAloneSigTable table)
        {
            //II.22.36

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("StandAloneSig Row", this, ViewKind.Metadata_StandAloneSigRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteBlobHeapIndex(nameof(Signature), Signature);

            return s.ToArray();
        }
    }
}
