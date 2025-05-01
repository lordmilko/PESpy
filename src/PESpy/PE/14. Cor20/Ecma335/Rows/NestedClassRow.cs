using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct NestedClassRow : IValue, IViewable
    {
        public int NestedClass { get; init; }

        public int EnclosingClass { get; init; }

        public RawOffset Offset { get; }

        internal static NestedClassRow New(MetadataReader metadataReader) => new NestedClassRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.NestedClass) + //NestedClass
            metadataReader.GetSimpleIndexSize(TableKind.NestedClass);  //EnclosingClass

        internal NestedClassRow(MetadataReader metadataReader)
        {
            //II.22.32

            Offset = (RawOffset) metadataReader.Position;

            NestedClass = metadataReader.ReadSimpleIndex(TableKind.NestedClass);
            EnclosingClass = metadataReader.ReadSimpleIndex(TableKind.NestedClass);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("NestedClass Row", this, ViewKind.Metadata_NestedClassRow);

            s.WriteSimpleIndex(nameof(NestedClass), NestedClass, TableKind.NestedClass);
            s.WriteSimpleIndex(nameof(EnclosingClass), EnclosingClass, TableKind.NestedClass);
        }
    }
}
