using PESpy.View;

namespace PESpy
{
    public readonly struct BlobEntry : IValue, IViewable
    {
        public byte[] CompressedSize { get; }

        public byte[] Bytes { get; }

        public int Offset { get; }

        public BlobEntry(int offset, byte[] compressedSize, byte[] bytes)
        {
            Offset = offset;
            CompressedSize = compressedSize;
            Bytes = bytes;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("BlobEntry", this, ViewKind.Metadata_Guid);

            s.WriteField("Size", CompressedSize);
            s.WriteField("Bytes", Bytes);
        }
    }
}
