using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct FieldRvaRow : IValue, IViewable
    {
        public int RVA { get; init; }

        public int Field { get; init; }

        public RawOffset Offset { get; }

        internal static FieldRvaRow New(MetadataReader metadataReader) => new FieldRvaRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                                       //RVA
            metadataReader.GetSimpleIndexSize(TableKind.Field); //Field

        internal FieldRvaRow(MetadataReader metadataReader)
        {
            //II.22.18

            Offset = (RawOffset) metadataReader.Position;

            RVA = metadataReader.ReadInt32();
            Field = metadataReader.ReadSimpleIndex(TableKind.Field);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("FieldRva Row", this, ViewKind.Metadata_FieldRvaRow);

            s.WriteValue(nameof(RVA), RVA);
            s.WriteSimpleIndex(nameof(Field), Field, TableKind.Field);
        }
    }
}
