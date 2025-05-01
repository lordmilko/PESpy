using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRefOSRow : IValue, IViewable
    {
        public int OSPlatformID { get; init; }
        public int OSMajorVersion { get; init; }
        public int OSMinorVersion { get; init; }

        public int AssemblyRef { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyRefOSRow New(MetadataReader metadataReader) => new AssemblyRefOSRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) + //OSPlatformID
            sizeof(short) + //OSMajorVersion
            sizeof(short) + //OSMinorVersion
            metadataReader.GetSimpleIndexSize(TableKind.AssemblyRef);

        internal AssemblyRefOSRow(MetadataReader metadataReader)
        {
            //II.22.6

            Offset = (RawOffset) metadataReader.Position;

            OSPlatformID = metadataReader.ReadInt16();
            OSMajorVersion = metadataReader.ReadInt16();
            OSMinorVersion = metadataReader.ReadInt16();

            AssemblyRef = metadataReader.ReadSimpleIndex(TableKind.AssemblyRef);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyRefOS Row", this, ViewKind.Metadata_AssemblyRefOSRow);

            s.WriteValue(nameof(OSPlatformID), OSPlatformID);
            s.WriteValue(nameof(OSMajorVersion), OSMajorVersion);
            s.WriteValue(nameof(OSMinorVersion), OSMinorVersion);

            s.WriteSimpleIndex(nameof(AssemblyRef), AssemblyRef, TableKind.AssemblyRef);
        }
    }
}
