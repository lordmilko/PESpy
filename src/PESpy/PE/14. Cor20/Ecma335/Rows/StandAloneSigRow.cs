using System;
using System.Diagnostics;
using PESpy.View;

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
            writer.NewStruct(Strings.StandAloneSigRow, this, ViewKind.Metadata_StandAloneSigRow, table.RowSize);

        int IViewable.NumChildren => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
