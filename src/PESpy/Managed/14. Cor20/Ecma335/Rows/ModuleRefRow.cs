using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ModuleRefRow : IValue, IViewable
    {
        public int Name { get; init; }

        public RawOffset Offset { get; }

        internal static ModuleRefRow New(MetadataReader metadataReader) => new ModuleRefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.StringIndexSize; //Name

        internal ModuleRefRow(MetadataReader metadataReader)
        {
            //II.22.31

            Offset = (RawOffset) metadataReader.Position;

            Name = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ModuleRef Row", this, ViewKind.Metadata_ModuleRefRow);

            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
