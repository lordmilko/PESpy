using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MemberRefRow : IValue, IViewable
    {
        public int Class { get; init; }

        public int Name { get; init; }

        public int Signature { get; init; }

        public RawOffset Offset { get; }

        internal static MemberRefRow New(MetadataReader metadataReader) => new MemberRefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.MemberRefParentSize + //Class
            metadataReader.StringIndexSize +     //Name
            metadataReader.BlobIndexSize;        //Signature

        internal MemberRefRow(MetadataReader metadataReader)
        {
            //II.22.25

            Offset = (RawOffset) metadataReader.Position;

            Class = metadataReader.ReadMemberRefParentIndex();
            Name = metadataReader.ReadStringHeapIndex();
            Signature = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MemberRef Row", this, ViewKind.Metadata_MemberRefRow);

            s.WriteMemberRefParentIndex(nameof(Class), Class);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
