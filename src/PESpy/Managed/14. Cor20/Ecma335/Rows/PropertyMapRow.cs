using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct PropertyMapRow : IValue, IViewable
    {
        public int Parent { get; init; }

        public int PropertyList { get; init; }

        public RawOffset Offset { get; }

        internal static PropertyMapRow New(MetadataReader metadataReader) => new PropertyMapRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef) + //Parent
            metadataReader.GetSimpleIndexSize(TableKind.Property); //PropertyList

        internal PropertyMapRow(MetadataReader metadataReader)
        {
            //II.22.35

            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
            PropertyList = metadataReader.ReadSimpleIndex(TableKind.Property);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("PropertyMap Row", this, ViewKind.Metadata_PropertyMapRow);

            s.WriteSimpleIndex(nameof(Parent), Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(PropertyList), PropertyList, TableKind.Property);
        }
    }
}
