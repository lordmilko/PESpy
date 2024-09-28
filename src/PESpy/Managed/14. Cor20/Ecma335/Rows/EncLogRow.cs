using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct EncLogRow : IValue, IViewable
    {
        public int Token { get; }
        public EditAndContinueOperation FuncCode { get; }

        public RawOffset Offset { get; }

        internal static EncLogRow New(MetadataReader metadataReader) => new EncLogRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) + //Token
            sizeof(int);  //FuncCode

        internal EncLogRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Token = metadataReader.ReadInt32();
            FuncCode = (EditAndContinueOperation) metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EncLog Row", this, ViewKind.Metadata_EncLogRow);

            s.WriteValue(nameof(Token), Token);
            s.WriteValue(nameof(FuncCode), FuncCode, sizeof(int));
        }
    }
}
