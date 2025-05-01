using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FieldPtrRow : IValue, IViewable
    {
        public int Field { get; }

        public RawOffset Offset { get; }

        internal static FieldPtrRow New(MetadataReader metadataReader) => new FieldPtrRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Field

        internal FieldPtrRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Field = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldPtr Row", this, ViewKind.Metadata_FieldPtrRow);

            s.WriteValue(nameof(Field), Field);
        }
    }
}
