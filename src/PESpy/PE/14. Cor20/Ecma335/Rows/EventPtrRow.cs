using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct EventPtrRow : IValue, IViewable
    {
        public int Event { get; }

        public RawOffset Offset { get; }

        internal static EventPtrRow New(MetadataReader metadataReader) => new EventPtrRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int);

        internal EventPtrRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Event = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EventPtr Row", this, ViewKind.Metadata_EventPtrRow);

            s.WriteValue(nameof(Event), Event);
        }
    }
}
