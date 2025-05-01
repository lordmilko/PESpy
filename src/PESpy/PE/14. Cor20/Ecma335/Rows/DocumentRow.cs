using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct DocumentRow : IValue, IViewable
    {
        public RawOffset Offset { get; }

        public int Name { get; init; }

        public int HashAlgorithm { get; init; }

        public int Hash { get; init; }

        public int Language { get; init; }

        internal static DocumentRow New(MetadataReader metadataReader) => new DocumentRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.BlobIndexSize + //Name
            metadataReader.GuidIndexSize + //HashAlgorithm
            metadataReader.BlobIndexSize + //Hash
            metadataReader.GuidIndexSize;  //Language

        internal DocumentRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#document-table-0x30

            Offset = (RawOffset) metadataReader.Position;

            Name = metadataReader.ReadBlobHeapIndex();
            HashAlgorithm = metadataReader.ReadGuidHeapIndex();
            Hash = metadataReader.ReadBlobHeapIndex();
            Language = metadataReader.ReadGuidHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Document Row", this, ViewKind.PortablePdb_DocumentRow);

            s.WriteBlobHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(HashAlgorithm), HashAlgorithm);
            s.WriteBlobHeapIndex(nameof(Hash), Hash);
            s.WriteGuidHeapIndex(nameof(Language), Language);
        }
    }
}
