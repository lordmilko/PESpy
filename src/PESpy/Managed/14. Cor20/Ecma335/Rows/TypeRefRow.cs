using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct TypeRefRow : IValue, IViewable
    {
        public int ResolutionScope { get; init; }

        public int TypeName { get; init; }

        public int TypeNamespace { get; init; }

        public RawOffset Offset { get; }

        internal static TypeRefRow New(MetadataReader metadataReader) => new TypeRefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.ResolutionScopeSize + //ResolutionScope
            metadataReader.StringIndexSize +     //TypeName
            metadataReader.StringIndexSize;      //TypeNamespace

        internal TypeRefRow(MetadataReader metadataReader)
        {
            //II.22.38

            Offset = (RawOffset) metadataReader.Position;

            ResolutionScope = metadataReader.ReadResolutionScopeIndex();
            TypeName = metadataReader.ReadStringHeapIndex();
            TypeNamespace = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("TypeRef Row", this, ViewKind.Metadata_TypeRefRow);

            s.WriteResolutionScopeIndex(nameof(ResolutionScope), ResolutionScope);
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
        }
    }
}
