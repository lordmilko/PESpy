namespace PESpy.PDB
{
    public readonly struct SPB
    {
        public ushort cSeg => chunk.PeekUInt16(0);

        public ushort pad => chunk.PeekUInt16(2);

        public NativeSpan<int> baseSrcLn => chunk.PeekNativeSpan<int>(4, cSeg);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) + //cSeg
            sizeof(short) + //pad
            cSeg * sizeof(int); //baseSrcLn

        private readonly MemoryChunk chunk;

        internal SPB(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
