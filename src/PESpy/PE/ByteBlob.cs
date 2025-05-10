using System;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a blob of bytes read from a file.
    /// </summary>
    public readonly struct ByteBlob : IValue, IViewable  //Small enough that returning a copy from properties is OK
    {
#if PEFAST
        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(0, length);
#else
        public byte[] Bytes { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;
        private readonly int length;

        internal ByteBlob(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }
#else
        internal ByteBlob(IFileReader reader, int length)
        {
            if (length == 0)
                throw new ArgumentException("Length should not be 0", nameof(length));

            Offset = (RawOffset) reader.Position;
            Bytes = reader.ReadBytes(length);
        }

        public ByteBlob(RawOffset fileOffset, byte[] bytes)
        {
            Offset = fileOffset;
            Bytes = bytes;
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteByteBlob(this);
        }
    }
}
