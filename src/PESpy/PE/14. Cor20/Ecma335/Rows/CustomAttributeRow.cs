using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public int Parent { get; init; }

        public int Type { get; init; }

        public int Value { get; init; }

        public RawOffset Offset { get; }

        internal static CustomAttributeRow New(MetadataReader metadataReader) => new CustomAttributeRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.HasCustomAttributeSize +  //Parent
            metadataReader.CustomAttributeTypeSize + //Type
            metadataReader.BlobIndexSize;            //Value

        internal CustomAttributeRow(MetadataReader metadataReader)
        {
            //II.22.10

            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadHasCustomAttributeIndex();
            Type = metadataReader.ReadCustomAttributeTypeIndex();
            Value = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("CustomAttribute Row", this, ViewKind.Metadata_CustomAttributeRow);

            s.WriteHasCustomAttributeIndex(nameof(Parent), Parent);
            s.WriteCustomAttributeTypeIndex(nameof(Type), Type);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
