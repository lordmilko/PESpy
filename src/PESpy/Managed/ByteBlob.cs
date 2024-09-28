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
        public byte[] Bytes { get; }

        public RawOffset Offset { get; }

        public ByteBlob(ref FileReader reader, int length)
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

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteByteBlob(this);
        }
    }
}
