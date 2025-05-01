using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct TypeSpecRow : IValue, IViewable
    {
        public int Signature { get; }

        public RawOffset Offset { get; }

        internal static TypeSpecRow New(MetadataReader metadataReader) => new TypeSpecRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.BlobIndexSize; //Signature

        internal TypeSpecRow(MetadataReader metadataReader)
        {
            //II.22.39

            Offset = (RawOffset) metadataReader.Position;

            Signature = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("TypeSpec Row", this, ViewKind.Metadata_TypeSpecRow);

            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
