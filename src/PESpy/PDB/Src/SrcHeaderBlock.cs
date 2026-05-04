using ClrDebug;

namespace PESpy
{
    public readonly struct SrcHeaderBlock
    {
        public int ver => chunk.PeekInt32(0);
        public int cb => chunk.PeekInt32(4);
        public FILETIME ft => chunk.PeekUnmanaged<FILETIME>(8);
        public int age => chunk.PeekInt32(16);
        public NativeSpan<byte> rgbPad => chunk.PeekNativeSpan<byte>(20, 44);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ver
            sizeof(int) + //cb
            8 + //ft
            sizeof(int) + //age
            44; //rgbPad

        private readonly MemoryChunk chunk;

        internal SrcHeaderBlock(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
