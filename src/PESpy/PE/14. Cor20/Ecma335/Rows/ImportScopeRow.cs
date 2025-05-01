using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ImportScopeRow : IValue, IViewable
    {
        public int Parent { get; }

        public int Imports { get; }

        public RawOffset Offset { get; }

        internal static ImportScopeRow New(MetadataReader metadataReader) => new ImportScopeRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                 //Parent
            metadataReader.BlobIndexSize; //Imports

        internal ImportScopeRow(MetadataReader metadataReader)
        {
            https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#importscope-table-0x35

            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadInt32();
            Imports = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ImportScope Row", this, ViewKind.PortablePdb_ImportScopeRow);

            s.WriteValue(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(Imports), Imports);
        }
    }
}
