using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct EventRow : IValue, IViewable
    {
        public CorEventAttr EventFlags { get; init; }

        public int Name { get; init; }

        public int EventType { get; init; }

        public RawOffset Offset { get; }

        internal static EventRow New(MetadataReader metadataReader) => new EventRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                  //EventFlags
            metadataReader.StringIndexSize + //Name
            metadataReader.TypeDefOrRefSize; //EventType

        internal EventRow(MetadataReader metadataReader)
        {
            //II.22.13

            Offset = (RawOffset) metadataReader.Position;

            EventFlags = (CorEventAttr) metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
            EventType = metadataReader.ReadTypeDefOrRefIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Event Row", this, ViewKind.Metadata_EventRow);

            s.WriteValue(nameof(EventFlags), EventFlags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteTypeDefOrRefIndex(nameof(EventType), EventType);
        }
    }
}
