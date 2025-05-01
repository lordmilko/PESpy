using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct EncMapRow : IValue, IViewable
    {
        public int Token { get; }

        public RawOffset Offset { get; }

        internal static EncMapRow New(MetadataReader metadataReader) => new EncMapRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Token

        internal EncMapRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Token = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EncMap Row", this, ViewKind.Metadata_EncMapRow);

            s.WriteValue(nameof(Token), Token);
        }
    }
}
