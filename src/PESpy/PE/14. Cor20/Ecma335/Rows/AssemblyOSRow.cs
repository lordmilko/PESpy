using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyOSRow : IValue, IViewable
    {
        public int OSPlatformID { get; init; }
        public int OSMajorVersion { get; init; }
        public int OSMinorVersion { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyOSRow New(MetadataReader metadataReader) => new AssemblyOSRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) + //OSPlatformID
            sizeof(int) + //OSMajorVersion
            sizeof(int);  //OSMinorVersion

        internal AssemblyOSRow(MetadataReader metadataReader)
        {
            //II.22.3

            Offset = (RawOffset) metadataReader.Position;

            OSPlatformID = metadataReader.ReadInt32();
            OSMajorVersion = metadataReader.ReadInt32();
            OSMinorVersion = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyOS Row", this, ViewKind.Metadata_AssemblyOSRow);

            s.WriteValue(nameof(OSPlatformID), OSPlatformID);
            s.WriteValue(nameof(OSMajorVersion), OSMajorVersion);
            s.WriteValue(nameof(OSMinorVersion), OSMinorVersion);
        }
    }
}
