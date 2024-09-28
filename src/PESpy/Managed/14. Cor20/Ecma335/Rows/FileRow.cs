using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FileRow : IValue, IViewable
    {
        public CorFileFlags Flags { get; init; }

        public int Name { get; init; }

        public int HashValue { get; init; }

        public RawOffset Offset { get; }

        internal static FileRow New(MetadataReader metadataReader) => new FileRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                    //Flags
            metadataReader.StringIndexSize + //Name
            metadataReader.BlobIndexSize;    //HashValue

        internal FileRow(MetadataReader metadataReader)
        {
            //II.22.19

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorFileFlags) metadataReader.ReadInt32();
            Name = metadataReader.ReadStringHeapIndex();
            HashValue = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("File Row", this, ViewKind.Metadata_FileRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(HashValue), HashValue);
        }
    }
}
