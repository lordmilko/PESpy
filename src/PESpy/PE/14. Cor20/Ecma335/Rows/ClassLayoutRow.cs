using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ClassLayoutRow : IValue, IViewable
    {
        public short PackingSize { get; init; }

        public int ClassSize { get; init; }

        public int Parent { get; init; }

        public RawOffset Offset { get; }

        internal static ClassLayoutRow New(MetadataReader metadataReader) => new ClassLayoutRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                                       //PackingSize
            sizeof(int) +                                         //ClassSize
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef); //Parent

        internal ClassLayoutRow(MetadataReader metadataReader)
        {
            //II.22.8

            Offset = (RawOffset) metadataReader.Position;

            PackingSize = metadataReader.ReadInt16();
            ClassSize = metadataReader.ReadInt32();
            Parent = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ClassLayout Row", this, ViewKind.Metadata_ClassLayoutRow);

            s.WriteValue(nameof(PackingSize), PackingSize);
            s.WriteValue(nameof(ClassSize), ClassSize);
            s.WriteSimpleIndex(nameof(Parent), Parent, TableKind.TypeDef);
        }
    }
}
