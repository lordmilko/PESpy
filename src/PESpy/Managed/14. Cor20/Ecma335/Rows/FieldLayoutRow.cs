using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FieldLayoutRow : IValue, IViewable
    {
        public int FieldOffset { get; init; }

        public int Field { get; init; }

        public RawOffset Offset { get; }

        internal static FieldLayoutRow New(MetadataReader metadataReader) => new FieldLayoutRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                                       //FieldOffset
            metadataReader.GetSimpleIndexSize(TableKind.Field); //Field

        internal FieldLayoutRow(MetadataReader metadataReader)
        {
            //II.22.16

            Offset = (RawOffset) metadataReader.Position;

            FieldOffset = metadataReader.ReadInt32();
            Field = metadataReader.ReadSimpleIndex(TableKind.Field);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldLayout Row", this, ViewKind.Metadata_FieldLayoutRow);

            s.WriteValue(nameof(FieldOffset), FieldOffset);
            s.WriteSimpleIndex(nameof(Field), Field, TableKind.Field);
        }
    }
}
