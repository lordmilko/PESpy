using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FieldRow : IValue, IViewable
    {
        public CorFieldAttr Flags { get; init; }

        public int Name { get; init; }

        public int Signature { get; init; }

        public RawOffset Offset { get; }

        internal static FieldRow New(MetadataReader metadataReader) => new FieldRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                  //Flags
            metadataReader.StringIndexSize + //Name
            metadataReader.BlobIndexSize;    //Signature

        internal FieldRow(MetadataReader metadataReader)
        {
            //II.22.15

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorFieldAttr) metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
            Signature = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Field Row", this, ViewKind.Metadata_FieldRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
