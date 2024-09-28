using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ParamPtrRow : IValue, IViewable
    {
        public int Param { get; }

        public RawOffset Offset { get; }

        internal static ParamPtrRow New(MetadataReader metadataReader) => new ParamPtrRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Param

        internal ParamPtrRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Param = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ParamPtr Row", this, ViewKind.Metadata_ParamPtrRow);

            s.WriteValue(nameof(Param), Param);
        }
    }
}
