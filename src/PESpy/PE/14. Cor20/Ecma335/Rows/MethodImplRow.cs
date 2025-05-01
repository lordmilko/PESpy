using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MethodImplRow : IValue, IViewable
    {
        public int Class { get; init; }

        public int MethodBody { get; init; }

        public int MethodDeclaration { get; init; }

        public RawOffset Offset { get; }

        internal static MethodImplRow New(MetadataReader metadataReader) => new MethodImplRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef) + //Class
            metadataReader.MethodDefOrRefSize +                    //MethodBody
            metadataReader.MethodDefOrRefSize;                     //MethodDeclaration

        internal MethodImplRow(MetadataReader metadataReader)
        {
            //II.22.27

            Offset = (RawOffset) metadataReader.Position;

            Class = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
            MethodBody = metadataReader.ReadMethodDefOrRefIndex();
            MethodDeclaration = metadataReader.ReadMethodDefOrRefIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodImpl Row", this, ViewKind.Metadata_MethodImplRow);

            s.WriteSimpleIndex(nameof(Class), Class, TableKind.TypeDef);
            s.WriteMethodDefOrRefIndex(nameof(MethodBody), MethodBody);
            s.WriteMethodDefOrRefIndex(nameof(MethodDeclaration), MethodDeclaration);
        }
    }
}
