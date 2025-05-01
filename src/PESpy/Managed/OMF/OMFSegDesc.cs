namespace PESpy
{
    public readonly struct OMFSegDesc : IValue
    {
        public ushort Seg => chunk.PeekUInt16(0);

        public ushort pad => chunk.PeekUInt16(2);

        public int Off => chunk.PeekInt32(4);

        public int cbSeg => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //Seg
            sizeof(ushort) + //pad
            sizeof(int) +    //Off
            sizeof(int);     //cbSeg

        private readonly MemoryChunk chunk;

        internal OMFSegDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
