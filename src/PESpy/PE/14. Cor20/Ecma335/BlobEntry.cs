using System;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly unsafe struct BlobEntry : IValue, IViewable
    {
        public NativeSpan<byte> CompressedSize => new NativeSpan<byte>(start, lengthSize);

        public NativeSpan<byte> Value => new NativeSpan<byte>(start + lengthSize, length);

        public int Offset { get; }

        private readonly byte* start;
        private readonly int length;
        private readonly byte lengthSize;

        public BlobEntry(int offset, byte* start, byte lengthSize, int length)
        {
            Offset = offset;
            this.start = start;
            this.lengthSize = lengthSize;
            this.length = length;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("BlobEntry", this, ViewKind.Metadata_Guid);

            s.WriteField("Size", CompressedSize);
            s.WriteField("Value", Value);
        }
    }
}
