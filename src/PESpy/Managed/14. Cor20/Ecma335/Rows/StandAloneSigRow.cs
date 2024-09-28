using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct StandAloneSigRow : IValue, IViewable
    {
        public int Signature { get; init; }

        public RawOffset Offset { get; }

        internal static StandAloneSigRow New(MetadataReader metadataReader) => new StandAloneSigRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.BlobIndexSize; //Signature

        internal StandAloneSigRow(MetadataReader metadataReader)
        {
            //II.22.36

            Offset = (RawOffset) metadataReader.Position;

            Signature = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("StandAloneSig Row", this, ViewKind.Metadata_StandAloneSigRow);

            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
