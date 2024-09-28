using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct PropertyPtrRow : IValue, IViewable
    {
        public int Property { get; }

        public RawOffset Offset { get; }

        internal static PropertyPtrRow New(MetadataReader metadataReader) => new PropertyPtrRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Property

        internal PropertyPtrRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Property = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("PropertyPtr Row", this, ViewKind.Metadata_PropertyPtrRow);

            s.WriteValue(nameof(Property), Property);
        }
    }
}
