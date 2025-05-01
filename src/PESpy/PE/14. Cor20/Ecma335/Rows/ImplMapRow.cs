using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ImplMapRow : IValue, IViewable
    {
        public CorPinvokeMap MappingFlags { get; init; }

        public int MemberForwarded { get; init; }

        public int ImportName { get; init; }

        public int ImportScope { get; init; }

        public RawOffset Offset { get; }

        internal static ImplMapRow New(MetadataReader metadataReader) => new ImplMapRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                                         //MappingFlags
            metadataReader.MemberForwardedSize +                    //MemberForwarded
            metadataReader.StringIndexSize +                        //ImportName
            metadataReader.GetSimpleIndexSize(TableKind.ModuleRef); //ImportScope

        internal ImplMapRow(MetadataReader metadataReader)
        {
            //II.22.22

            Offset = (RawOffset) metadataReader.Position;

            MappingFlags = (CorPinvokeMap) metadataReader.ReadInt16();
            MemberForwarded = metadataReader.ReadMemberForwardedIndex();
            ImportName = metadataReader.ReadStringHeapIndex();
            ImportScope = metadataReader.ReadSimpleIndex(TableKind.ModuleRef);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ImplMap Row", this, ViewKind.Metadata_ImplMapRow);

            s.WriteValue(nameof(MappingFlags), MappingFlags, sizeof(short));
            s.WriteMemberForwardedIndex(nameof(MemberForwarded), MemberForwarded);
            s.WriteStringHeapIndex(nameof(ImportName), ImportName);
            s.WriteSimpleIndex(nameof(ImportScope), ImportScope, TableKind.ModuleRef);
        }
    }
}
