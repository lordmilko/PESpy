using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}, Instantiation = {Instantiation}")]
    public readonly struct MethodSpecRow : IValue, IViewable
    {
        public MethodSpecIndex RowIndex { get; }

        public Index Method => table.GetMethod(RowIndex);

        public BlobIndex Instantiation => table.GetInstantiation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodSpecTable table;

        internal MethodSpecRow(MethodSpecIndex index, MethodSpecTable table)
        {
            //II.22.29

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodSpec Row", this, ViewKind.Metadata_MethodSpecRow);

            s.WriteMethodDefOrRefIndex(nameof(Method), (int) Method);
            s.WriteBlobHeapIndex(nameof(Instantiation), Instantiation);
        }
    }
}
