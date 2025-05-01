using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct InterfaceImplRow : IValue, IViewable
    {
        public int Class { get; init; }

        public int Interface { get; init; }

        public RawOffset Offset { get; }

        internal static InterfaceImplRow New(MetadataReader metadataReader) => new InterfaceImplRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef) + //Class
            metadataReader.TypeDefOrRefSize;                       //Interface

        internal InterfaceImplRow(MetadataReader metadataReader)
        {
            //II.22.23

            Offset = (RawOffset) metadataReader.Position;

            Class = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
            Interface = metadataReader.ReadTypeDefOrRefIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("InterfaceImpl Row", this, ViewKind.Metadata_InterfaceImplRow);

            s.WriteSimpleIndex(nameof(Class), Class, TableKind.TypeDef);
            s.WriteTypeDefOrRefIndex(nameof(Interface), Interface);
        }
    }
}
