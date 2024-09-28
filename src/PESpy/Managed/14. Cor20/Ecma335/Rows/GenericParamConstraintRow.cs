using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct GenericParamConstraintRow : IValue, IViewable
    {
        public int Owner { get; init; }

        public int Constraint { get; init; }

        public RawOffset Offset { get; }

        internal static GenericParamConstraintRow New(MetadataReader metadataReader) => new GenericParamConstraintRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            metadataReader.GetSimpleIndexSize(TableKind.GenericParam) + //Owner
            metadataReader.TypeDefOrRefSize;                            //Constraint

        internal GenericParamConstraintRow(MetadataReader metadataReader)
        {
            //II.22.21

            Offset = (RawOffset) metadataReader.Position;

            Owner = metadataReader.ReadSimpleIndex(TableKind.GenericParam);
            Constraint = metadataReader.ReadTypeDefOrRefIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("GenericParamConstraint Row", this, ViewKind.Metadata_GenericParamConstraintRow);

            s.WriteSimpleIndex(nameof(Owner), Owner, TableKind.GenericParam);
            s.WriteTypeDefOrRefIndex(nameof(Constraint), Constraint);
        }
    }
}
