using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct LocalConstantRow : IValue, IViewable
    {
        public int Name { get; }

        public int Signature { get; }

        public RawOffset Offset { get; }

        internal static LocalConstantRow New(MetadataReader metadataReader) => new LocalConstantRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.StringIndexSize + //Name
            metadataReader.BlobIndexSize;    //Signature

        internal LocalConstantRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localconstant-table-0x34

            Offset = (RawOffset) metadataReader.Position;

            Name = metadataReader.ReadStringHeapIndex();
            Signature = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("LocalConstant Row", this, ViewKind.PortablePdb_LocalConstantRow);

            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
