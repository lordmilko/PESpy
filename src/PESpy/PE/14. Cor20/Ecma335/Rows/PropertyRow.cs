using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct PropertyRow : IValue, IViewable
    {
        public CorPropertyAttr Flags { get; init; }

        public int Name { get; init; }

        public int Type { get; init; }

        public RawOffset Offset { get; }

        internal static PropertyRow New(MetadataReader metadataReader) => new PropertyRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                  //Flags
            metadataReader.StringIndexSize + //Name
            metadataReader.BlobIndexSize;    //Type

        internal PropertyRow(MetadataReader metadataReader)
        {
            //II.22.34

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorPropertyAttr) metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
            Type = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Property Row", this, ViewKind.Metadata_PropertyRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Type), Type);
        }
    }
}
