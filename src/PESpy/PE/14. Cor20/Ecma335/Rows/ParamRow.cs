using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ParamRow : IValue, IViewable
    {
        public CorParamAttr Flags { get; init; }

        public short Sequence { get; init; }

        public int Name { get; init; }

        public RawOffset Offset { get; }

        internal static ParamRow New(MetadataReader metadataReader) => new ParamRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                 //Flags
            sizeof(short) +                 //Sequence
            metadataReader.StringIndexSize; //Name

        internal ParamRow(MetadataReader metadataReader)
        {
            //II.22.33

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorParamAttr) metadataReader.ReadInt16();
            Sequence = metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Param Row", this, ViewKind.Metadata_ParamRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteValue(nameof(Sequence), Sequence);
            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
