using System;

namespace PESpy
{
    public readonly struct OMFSourceLine
    {
        public ushort Seg => chunk.PeekUInt16(0);

        public ushort cLnOff => chunk.PeekUInt16(2);

        public Span<int> offset => chunk.PeekSpan<int>(4, cLnOff);

        public Span<ushort> lineNbr
        {
            get
            {
                var count = cLnOff;
                return chunk.PeekSpan<ushort>(4 + (count * 4), count);
            }
        }

        private readonly MemoryChunk chunk;

        internal OMFSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
