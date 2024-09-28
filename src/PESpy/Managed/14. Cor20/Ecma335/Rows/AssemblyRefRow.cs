using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRefRow : IValue, IViewable
    {
        public short MajorVersion { get; init; }
        public short MinorVersion { get; init; }
        public short BuildNumber { get; init; }
        public short RevisionNumber { get; init; }

        public AssemblyFlags Flags { get; init; }

        public int PublicKeyOrToken { get; init; }

        public int Name { get; init; }

        public int Culture { get; init; }

        public int HashValue { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyRefRow New(MetadataReader metadataReader) => new AssemblyRefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                  //MajorVersion
            sizeof(short) +                  //MinorVersion
            sizeof(short) +                  //BuildNumber
            sizeof(short) +                  //RevisionNumber
            sizeof(int) +                    //Flags
            metadataReader.BlobIndexSize +   //PublicKeyOrToken
            metadataReader.StringIndexSize + //Name
            metadataReader.StringIndexSize + //Culture
            metadataReader.BlobIndexSize;    //HashValue

        internal AssemblyRefRow(MetadataReader metadataReader)
        {
            //II.22.5

            Offset = (RawOffset) metadataReader.Position;

            MajorVersion = metadataReader.ReadInt16();
            MinorVersion = metadataReader.ReadInt16();
            BuildNumber = metadataReader.ReadInt16();
            RevisionNumber = metadataReader.ReadInt16();
            Flags = (AssemblyFlags) metadataReader.ReadInt32();
            PublicKeyOrToken = metadataReader.ReadBlobHeapIndex();
            Name = metadataReader.ReadStringHeapIndex();
            Culture = metadataReader.ReadStringHeapIndex();
            HashValue = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyRef Row", this, ViewKind.Metadata_AssemblyRefRow);

            s.WriteValue(nameof(MajorVersion), MajorVersion);
            s.WriteValue(nameof(MinorVersion), MinorVersion);
            s.WriteValue(nameof(BuildNumber), BuildNumber);
            s.WriteValue(nameof(RevisionNumber), RevisionNumber);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteBlobHeapIndex(nameof(PublicKeyOrToken), PublicKeyOrToken);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteStringHeapIndex(nameof(Culture), Culture);
            s.WriteBlobHeapIndex(nameof(HashValue), HashValue);
        }
    }
}
