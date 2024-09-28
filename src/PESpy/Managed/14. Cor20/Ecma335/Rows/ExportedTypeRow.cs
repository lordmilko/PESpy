using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct ExportedTypeRow : IValue, IViewable
    {
        public CorTypeAttr Flags { get; init; }

        public int TypeDefId { get; init; }

        public int TypeName { get; init; }

        public int TypeNamespace { get; init; }

        public int Implementation { get; init; }

        public RawOffset Offset { get; }

        internal static ExportedTypeRow New(MetadataReader metadataReader) => new ExportedTypeRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                                          //Flags
            metadataReader.GetSimpleIndexSize(TableKind.TypeDef) + //TypeDefId
            metadataReader.StringIndexSize +                       //TypeName
            metadataReader.StringIndexSize +                       //TypeNamespace
            metadataReader.ImplementationSize;                     //Implementation

        internal ExportedTypeRow(MetadataReader metadataReader)
        {
            //II.22.14

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorTypeAttr) metadataReader.ReadInt32();
            TypeDefId = metadataReader.ReadSimpleIndex(TableKind.TypeDef);
            TypeName = metadataReader.ReadStringHeapIndex();
            TypeNamespace = metadataReader.ReadStringHeapIndex();
            Implementation = metadataReader.ReadImplementationIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ExportedType Row", this, ViewKind.Metadata_ExportedTypeRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteSimpleIndex(nameof(TypeDefId), TypeDefId, TableKind.TypeDef);
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
            s.WriteImplementationIndex(nameof(Implementation), Implementation);
        }
    }
}
