using PESpy.Ecma335;

namespace PESpy
{
    //Type is made up
    public readonly struct ManifestResource
    {
        public ManifestResourceRow Row { get; }

        public int Size => chunk.PeekInt32(0);

        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(4, Size);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ManifestResource(in ManifestResourceRow row, in MemoryChunk chunk)
        {
            Row = row;
            this.chunk = chunk;
        }
    }
}
