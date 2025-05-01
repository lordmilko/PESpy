using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MethodDebugInformationRow : IValue, IViewable
    {
        public int Document { get; init; }

        public int SequencePoints { get; init; }

        public RawOffset Offset { get; }

        internal static MethodDebugInformationRow New(MetadataReader metadataReader) => new MethodDebugInformationRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                 //Document
            metadataReader.BlobIndexSize; //SequencePoints

        internal MethodDebugInformationRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#methoddebuginformation-table-0x31

            Offset = (RawOffset) metadataReader.Position;

            Document = metadataReader.ReadInt32();
            SequencePoints = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodDebugInformation Row", this, ViewKind.PortablePdb_MethodDebugInformationRow);

            s.WriteValue(nameof(Document), Document);
            s.WriteBlobHeapIndex(nameof(SequencePoints), SequencePoints);
        }
    }
}
