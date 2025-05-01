using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MethodSemanticsRow : IValue, IViewable
    {
        public CorMethodSemanticsAttr Semantics { get; init; }

        public int Method { get; init; }

        public int Association { get; init; }

        public RawOffset Offset { get; }

        internal static MethodSemanticsRow New(MetadataReader metadataReader) => new MethodSemanticsRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                                          //Semantics
            metadataReader.GetSimpleIndexSize(TableKind.MethodDef) + //Method
            metadataReader.HasSemanticsSize;                         //Association

        internal MethodSemanticsRow(MetadataReader metadataReader)
        {
            //II.22.28

            Offset = (RawOffset) metadataReader.Position;

            Semantics = (CorMethodSemanticsAttr) metadataReader.ReadInt16();
            Method = metadataReader.ReadSimpleIndex(TableKind.MethodDef);
            Association = metadataReader.ReadHasSemanticsIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodSemantics Row", this, ViewKind.Metadata_MethodSemanticsRow);

            s.WriteValue(nameof(Semantics), Semantics, sizeof(short));
            s.WriteSimpleIndex(nameof(Method), Method, TableKind.MethodDef);
            s.WriteHasSemanticsIndex(nameof(Association), Association);
        }
    }
}
