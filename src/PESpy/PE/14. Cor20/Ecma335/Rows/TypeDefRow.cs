using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct TypeDefRow : IValue, IViewable
    {
        public CorTypeAttr Flags { get; init; }

        public int TypeName { get; init; }

        public int TypeNamespace { get; init; }

        public int Extends { get; init; }

        public int FieldList { get; init; }

        public int MethodList { get; init; }

        public RawOffset Offset { get; }

        internal static TypeDefRow New(MetadataReader metadataReader) => new TypeDefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) + //Flags
            metadataReader.StringIndexSize +                        //TypeName
            metadataReader.StringIndexSize +                        //TypeNamespace
            metadataReader.TypeDefOrRefSize +                       //Extends
            metadataReader.GetSimpleIndexSize(TableKind.Field) +    //FieldList
            metadataReader.GetSimpleIndexSize(TableKind.MethodDef); //MethodList

        internal TypeDefRow(MetadataReader metadataReader)
        {
            //II.22.37

            Offset = (RawOffset) metadataReader.Position;

            Flags = (CorTypeAttr) metadataReader.ReadInt32();
            TypeName = metadataReader.ReadStringHeapIndex();
            TypeNamespace = metadataReader.ReadStringHeapIndex();
            Extends = metadataReader.ReadTypeDefOrRefIndex();
            FieldList = metadataReader.ReadSimpleIndex(TableKind.Field);
            MethodList = metadataReader.ReadSimpleIndex(TableKind.MethodDef);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("TypeDef Row", this, ViewKind.Metadata_TypeDefRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
            s.WriteTypeDefOrRefIndex(nameof(Extends), Extends);
            s.WriteSimpleIndex(nameof(FieldList), FieldList, TableKind.Field);
            s.WriteSimpleIndex(nameof(MethodList), MethodList, TableKind.MethodDef);
        }
    }
}
