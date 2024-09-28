using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct CustomDebugInformationRow : IValue, IViewable
    {
        public int Parent { get; }

        public int Kind { get; }

        public int Value { get; }

        public RawOffset Offset { get; }

        internal static CustomDebugInformationRow New(MetadataReader metadataReader) => new CustomDebugInformationRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.HasCustomDebugInformationSize + //Parent
            metadataReader.GuidIndexSize +                 //Kind
            metadataReader.BlobIndexSize;                  //Value

        internal CustomDebugInformationRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadHasCustomDebugInformationIndex();
            Kind = metadataReader.ReadGuidHeapIndex();
            Value = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("CustomDebugInformation Row", this, ViewKind.PortablePdb_CustomDebugInformationRow);

            s.WriteHasCustomDebugInformationIndex(nameof(Parent), Parent);
            s.WriteGuidHeapIndex(nameof(Kind), Kind);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
