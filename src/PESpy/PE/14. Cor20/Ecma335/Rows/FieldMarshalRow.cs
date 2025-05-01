using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FieldMarshalRow : IValue, IViewable
    {
        public int Parent { get; init; }

        public int NativeType { get; init; }

        public RawOffset Offset { get; }

        internal static FieldMarshalRow New(MetadataReader metadataReader) => new FieldMarshalRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.HasFieldMarshalSize + //Parent
            metadataReader.BlobIndexSize;

        internal FieldMarshalRow(MetadataReader metadataReader)
        {
            //II.22.17

            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadHasFieldMarshalIndex();
            NativeType = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldMarshal Row", this, ViewKind.Metadata_FieldMarshalRow);

            s.WriteHasFieldMarshalIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(NativeType), NativeType);
        }
    }
}
