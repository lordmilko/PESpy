using System.Configuration.Assemblies;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRow : IValue, IViewable
    {
        public AssemblyHashAlgorithm HashAlgId { get; init; }

        public short MajorVersion { get; init; }
        public short MinorVersion { get; init; }
        public short BuildNumber { get; init; }
        public short RevisionNumber { get; init; }

        public AssemblyFlags Flags { get; init; }

        public int PublicKey { get; init; }

        public int Name { get; init; }

        public int Culture { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyRow New(MetadataReader metadataReader) => new AssemblyRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                    //HashAlgId
            sizeof(short) +                  //MajorVersion
            sizeof(short) +                  //MinorVersion
            sizeof(short) +                  //BuildNumber
            sizeof(short) +                  //RevisionNumber
            sizeof(int) +                    //Flags
            metadataReader.BlobIndexSize +   //PublicKey
            metadataReader.StringIndexSize + //Name
            metadataReader.StringIndexSize;  //Culture

        internal AssemblyRow(MetadataReader metadataReader)
        {
            //II.22.2

            Offset = (RawOffset) metadataReader.Position;

            HashAlgId = (AssemblyHashAlgorithm) metadataReader.ReadInt32();
            MajorVersion = metadataReader.ReadInt16();
            MinorVersion = metadataReader.ReadInt16();
            BuildNumber = metadataReader.ReadInt16();
            RevisionNumber = metadataReader.ReadInt16();
            Flags = (AssemblyFlags) metadataReader.ReadInt32();
            PublicKey = metadataReader.ReadBlobHeapIndex();
            Name = metadataReader.ReadStringHeapIndex();
            Culture = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Assembly Row", this, ViewKind.Metadata_AssemblyRow);

            s.WriteValue(nameof(HashAlgId), HashAlgId, sizeof(int));
            s.WriteValue(nameof(MajorVersion), MajorVersion);
            s.WriteValue(nameof(MinorVersion), MinorVersion);
            s.WriteValue(nameof(BuildNumber), BuildNumber);
            s.WriteValue(nameof(RevisionNumber), RevisionNumber);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteBlobHeapIndex(nameof(PublicKey), PublicKey);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteStringHeapIndex(nameof(Culture), Culture);
        }
    }
}
