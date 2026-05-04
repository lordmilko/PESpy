using System;

namespace PESpy
{
    public readonly struct ImageEpilogueDynamicRelocationHeader : IValue
    {
        public int EpilogueCount => chunk.PeekInt32(0);
        public byte EpilogueByteCount => chunk.PeekByte(4);
        public byte BranchDescriptorElementSize => chunk.PeekByte(5);
        public short BranchDescriptorCount => chunk.PeekInt16(6);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageEpilogueDynamicRelocationHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
