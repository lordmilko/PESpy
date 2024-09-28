using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct DeclSecurityRow : IValue, IViewable
    {
        //SecurityAction does not have all of the values that CorDeclSecurity has
        public CorDeclSecurity Action { get; init; }

        public int Parent { get; init; }

        public int PermissionSet { get; init; }

        public RawOffset Offset { get; }

        internal static DeclSecurityRow New(MetadataReader metadataReader) => new DeclSecurityRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(short) +                      //Action
            metadataReader.HasDeclSecuritySize + //Parent
            metadataReader.BlobIndexSize;        //PermissionSet

        internal DeclSecurityRow(MetadataReader metadataReader)
        {
            //II.22.11

            Offset = (RawOffset) metadataReader.Position;

            Action = (CorDeclSecurity) metadataReader.ReadInt16();
            Parent = metadataReader.ReadHasDeclSecurityIndex();
            PermissionSet = metadataReader.ReadBlobHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("DeclSecurity Row", this, ViewKind.Metadata_DeclSecurityRow);

            s.WriteValue(nameof(Action), Action, sizeof(short));
            s.WriteHasDeclSecurityIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(PermissionSet), PermissionSet);
        }
    }
}
