using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRefProcessorRow : IValue, IViewable
    {
        public int Processor { get; init; }

        public int AssemblyRef { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyRefProcessorRow New(MetadataReader metadataReader) => new AssemblyRefProcessorRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                                             //Processor
            metadataReader.GetSimpleIndexSize(TableKind.AssemblyRef); //AssemblyRef

        internal AssemblyRefProcessorRow(MetadataReader metadataReader)
        {
            //II.22.7

            Offset = (RawOffset) metadataReader.Position;

            Processor = metadataReader.ReadInt32();
            AssemblyRef = metadataReader.ReadSimpleIndex(TableKind.AssemblyRef);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyRefProcessor Row", this, ViewKind.Metadata_AssemblyRefProcessorRow);

            s.WriteValue(nameof(Processor), Processor);
            s.WriteSimpleIndex(nameof(AssemblyRef), AssemblyRef, TableKind.AssemblyRef);
        }
    }
}
