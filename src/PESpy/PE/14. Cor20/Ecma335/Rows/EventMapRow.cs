using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct EventMapRow : IValue, IViewable
    {
        public int Parent { get; init; }

        public int EventList { get; init; }

        public RawOffset Offset { get; }

        internal static EventMapRow New(MetadataReader metadataReader) => new EventMapRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef) + //Parent
            metadataReader.GetSimpleIndexSize(TableKind.Event);    //EventList

        internal EventMapRow(MetadataReader metadataReader)
        {
            //II.22.12

            Offset = (RawOffset) metadataReader.Position;

            Parent = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
            EventList = metadataReader.ReadSimpleIndex(TableKind.Event);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("EventMap Row", this, ViewKind.Metadata_EventMapRow);

            s.WriteSimpleIndex(nameof(Parent), Parent, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(EventList), EventList, TableKind.Event);
        }
    }
}
