namespace PESpy.PDB
{
    //mli.cpp has names it uses when it emits the data, but mod.cpp has some well formed struct names that we use
    //FSB is the "header" information
    public readonly struct FSB
    {
        public ushort cFile => chunk.PeekUInt16(0);

        public ushort cSeg => chunk.PeekUInt16(2);

        public NativeSpan<int> baseSrcFile => chunk.PeekNativeSpan<int>(4, cFile);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) + //cFile
            sizeof(short) + //cSeg
            cFile * sizeof(int); //baseSrcFile

        private readonly MemoryChunk chunk;

        internal FSB(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
