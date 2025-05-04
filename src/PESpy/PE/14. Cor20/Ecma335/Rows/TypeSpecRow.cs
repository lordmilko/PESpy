using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Signature = {Signature}")]
    public readonly struct TypeSpecRow : IValue, IViewable
    {
        public TypeSpecIndex RowIndex { get; }

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly TypeSpecTable table;

        internal TypeSpecRow(TypeSpecIndex index, TypeSpecTable table)
        {
            //II.22.39

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("TypeSpec Row", this, ViewKind.Metadata_TypeSpecRow);

            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
