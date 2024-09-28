using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct AssemblyProcessorRow : IValue, IViewable
    {
        public int Processor { get; init; }

        public RawOffset Offset { get; }

        internal static AssemblyProcessorRow New(MetadataReader metadataReader) => new AssemblyProcessorRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Processor

        internal AssemblyProcessorRow(MetadataReader metadataReader)
        {
            //II.22.4

            Offset = (RawOffset) metadataReader.Position;

            Processor = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyProcessor Row", this, ViewKind.Metadata_AssemblyProcessorRow);

            s.WriteValue(nameof(Processor), Processor);
        }
    }
}
