using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ManifestResourceRow : IValue, IViewable
    {
        public int ResourceOffset { get; init; }

        public CorManifestResourceFlags Flags { get; init; }

        public int Name { get; init; }

        public int Implementation { get; init; }

        public RawOffset Offset { get; }

        internal static ManifestResourceRow New(MetadataReader metadataReader) => new ManifestResourceRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                      //ResourceOffset
            sizeof(int) +                      //Flags
            metadataReader.StringIndexSize +   //Name
            metadataReader.ImplementationSize; //Implementation

        internal ManifestResourceRow(MetadataReader metadataReader)
        {
            //II.22.24

            Offset = (RawOffset) metadataReader.Position;

            ResourceOffset = metadataReader.ReadInt32();
            Flags = (CorManifestResourceFlags) metadataReader.ReadInt32();
            Name = metadataReader.ReadStringHeapIndex();
            Implementation = metadataReader.ReadImplementationIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ManifestResource Row", this, ViewKind.Metadata_ManifestResourceRow);

            s.WriteValue(nameof(ResourceOffset), ResourceOffset);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteImplementationIndex(nameof(Implementation), Implementation);
        }
    }
}
