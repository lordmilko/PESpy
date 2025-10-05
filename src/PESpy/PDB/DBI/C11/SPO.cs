namespace PESpy.PDB
{
    public readonly struct SPO
    {
        public short Seg => chunk.PeekInt16(0);

        public short cPair => chunk.PeekInt16(2);

        //SPO contains an int[] called "offset", however this is erroneous, because the buffer contains two pieces of information:
        //an int[] of offsets, and then a short[] of line numbers
        public NativeSpan<int> offset => chunk.PeekNativeSpan<int>(4, cPair);

        public NativeSpan<ushort> linenumbers => chunk.PeekNativeSpan<ushort>(4 + (cPair * sizeof(int)), cPair);

        private readonly MemoryChunk chunk;

        internal SPO(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
