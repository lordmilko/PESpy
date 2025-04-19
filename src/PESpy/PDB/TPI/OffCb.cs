namespace PESpy.PDB
{
    public readonly struct OffCb
    {
        public int off => chunk.PeekInt32(0);

        public int cb => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OffCb(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
