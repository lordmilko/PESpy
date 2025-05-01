using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, ImplFlags = {ImplFlags}, Flags = {Flags}")]
    public readonly struct MethodDefRow : IValue, IViewable
    {
        public int RVA { get; init; }

        public CorMethodImpl ImplFlags { get; init; }

        public CorMethodAttr Flags { get; init; }

        public int Name { get; init; }

        public int Signature { get; init; }

        public int ParamList { get; init; }

        public RawOffset Offset { get; }

        internal static MethodDefRow New(MetadataReader metadataReader) => new MethodDefRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                                       //RVA
            sizeof(short) +                                     //ImplFlags
            sizeof(short) +                                     //Flags
            metadataReader.StringIndexSize +                    //Name
            metadataReader.BlobIndexSize +                      //Signature
            metadataReader.GetSimpleIndexSize(TableKind.Param); //ParamList

        internal MethodDefRow(MetadataReader metadataReader)
        {
            //II.22.26

            Offset = (RawOffset) metadataReader.Position;

            RVA = metadataReader.ReadInt32();
            ImplFlags = (CorMethodImpl) metadataReader.ReadInt16();
            Flags = (CorMethodAttr) metadataReader.ReadInt16();
            Name = metadataReader.ReadStringHeapIndex();
            Signature = metadataReader.ReadBlobHeapIndex();
            ParamList = metadataReader.ReadSimpleIndex(TableKind.Param);

            //We don't store the ImageCorILMethodInfo on this object, as that will cause our FileReader cache to be invalidated.
            //We store the IL methods directly on the PEFile instead
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodDef Row", this, ViewKind.Metadata_MethodDefRow);

            s.WriteValue(nameof(RVA), RVA);
            s.WriteValue(nameof(ImplFlags), ImplFlags, sizeof(short));
            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
            s.WriteSimpleIndex(nameof(ParamList), ParamList, TableKind.Param);
        }
    }
}
