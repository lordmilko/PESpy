namespace PESpy
{
    public readonly struct OMFDirHeader : IValue
    {
        public ushort cbDirHeader => chunk.PeekUInt16(0);

        public ushort cbDirEntry => chunk.PeekUInt16(2);

        public int cDir => chunk.PeekInt32(4);

        public int lfoNextDir => chunk.PeekInt32(8);

        public int flags => chunk.PeekInt32(12);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //cbDirHeader
            sizeof(ushort) + //cbDirEntry
            sizeof(int) + //cDir
            sizeof(int) + //lfoNextDir
            sizeof(int); //flags

        private readonly MemoryChunk chunk;

        internal OMFDirHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
