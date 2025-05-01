using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ConstantRow : IValue, IViewable
    {
        public CorElementType Type { get; init; }

        public byte Padding { get; init; }

        public int Parent { get; init; }

        public int Value { get; init; }

        public RawOffset Offset { get; }

        internal static ConstantRow New(MetadataReader metadataReader) => new ConstantRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(byte) +                   //Type
            sizeof(byte) +                   //Padding
            metadataReader.HasConstantSize + //Parent
            metadataReader.BlobIndexSize;    //Value

        internal ConstantRow(MetadataReader metadataReader)
        {
            //II.22.9

            Offset = (RawOffset) metadataReader.Position;

            Type = (CorElementType) metadataReader.ReadByte();
            Padding = metadataReader.ReadByte();
            Parent = metadataReader.ReadHasConstantIndex();
            Value = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Constant Row", this, ViewKind.Metadata_ConstantRow);

            s.WriteValue(nameof(Type), Type, sizeof(byte));
            s.WriteValue(nameof(Padding), Padding);
            s.WriteHasConstantIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
