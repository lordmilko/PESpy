using System;

namespace PESpy
{
    public readonly struct OMFSourceLine
    {
        public ushort Seg => chunk.PeekUInt16(0);

        public ushort cLnOff => chunk.PeekUInt16(2);

        public NativeSpan<int> offset => chunk.PeekNativeSpan<int>(4, cLnOff);

        public NativeSpan<ushort> lineNbr
        {
            get
            {
                var count = cLnOff;
                return chunk.PeekNativeSpan<ushort>(4 + (count * 4), count);
            }
        }

        private readonly MemoryChunk chunk;

        internal OMFSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
