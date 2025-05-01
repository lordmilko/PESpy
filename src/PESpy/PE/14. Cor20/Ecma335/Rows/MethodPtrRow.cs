using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct MethodPtrRow : IValue, IViewable
    {
        public int Method { get; }

        public RawOffset Offset { get; }

        internal static MethodPtrRow New(MetadataReader metadataReader) => new MethodPtrRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int); //Method

        internal MethodPtrRow(MetadataReader metadataReader)
        {
            Offset = (RawOffset) metadataReader.Position;

            Method = metadataReader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodPtr Row", this, ViewKind.Metadata_MethodPtrRow);

            s.WriteValue(nameof(Method), Method);
        }
    }
}
