using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ModuleRow : IValue, IViewable
    {
        public short Generation { get; init; }

        public int Name { get; init; }

        public int Mvid { get; init; }

        public int EncId { get; init; }

        public int EncBaseId { get; init; }

        public RawOffset Offset { get; }

        internal static ModuleRow New(MetadataReader metadataReader) => new ModuleRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                  //Generation
            metadataReader.StringIndexSize + //Name
            metadataReader.GuidIndexSize +   //Mvid
            metadataReader.GuidIndexSize +   //EncId
            metadataReader.GuidIndexSize;    //EncBaseId

        internal ModuleRow(MetadataReader metadataReader)
        {
            //II.22.30

            Offset = (RawOffset) metadataReader.Position;

            Generation = metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
            Mvid = metadataReader.ReadGuidHeapIndex();
            EncId = metadataReader.ReadGuidHeapIndex();
            EncBaseId = metadataReader.ReadGuidHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Module Row", this, ViewKind.Metadata_ModuleRow);

            s.WriteValue(nameof(Generation), Generation);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(Mvid), Mvid);
            s.WriteGuidHeapIndex(nameof(EncId), EncId);
            s.WriteGuidHeapIndex(nameof(EncBaseId), EncBaseId);
        }
    }
}
