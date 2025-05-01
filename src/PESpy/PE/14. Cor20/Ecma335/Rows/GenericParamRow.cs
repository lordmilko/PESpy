using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct GenericParamRow : IValue, IViewable
    {
        public short Number { get; init; }

        public CorGenericParamAttr Flags { get; init; }

        public int Owner { get; init; }

        public int Name { get; init; }

        public RawOffset Offset { get; }

        internal static GenericParamRow New(MetadataReader metadataReader) => new GenericParamRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                      //Number
            sizeof(short) +                      //Flags
            metadataReader.TypeOrMethodDefSize + //Owner
            metadataReader.StringIndexSize;      //Name

        internal GenericParamRow(MetadataReader metadataReader)
        {
            //II.22.20

            Offset = (RawOffset) metadataReader.Position;

            Number = metadataReader.ReadInt16();
            Flags = (CorGenericParamAttr) metadataReader.ReadInt16();
            Owner = metadataReader.ReadTypeOrMethodDefIndex();
            Name = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("GenericParam Row", this, ViewKind.Metadata_GenericParamRow);

            s.WriteValue(nameof(Number), Number);
            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteTypeOrMethodDefIndex(nameof(Owner), Owner);
            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
