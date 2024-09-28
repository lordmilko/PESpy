using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MethodSpecRow : IValue, IViewable
    {
        public int Method { get; init; }

        public int Instantiation { get; init; }

        public RawOffset Offset { get; }

        internal static MethodSpecRow New(MetadataReader metadataReader) => new MethodSpecRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.MethodDefOrRefSize + //Method
            metadataReader.BlobIndexSize;       //Instantiation

        internal MethodSpecRow(MetadataReader metadataReader)
        {
            //II.22.29

            Offset = (RawOffset) metadataReader.Position;

            Method = metadataReader.ReadMethodDefOrRefIndex();
            Instantiation = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodSpec Row", this, ViewKind.Metadata_MethodSpecRow);

            s.WriteMethodDefOrRefIndex(nameof(Method), Method);
            s.WriteBlobHeapIndex(nameof(Instantiation), Instantiation);
        }
    }
}
