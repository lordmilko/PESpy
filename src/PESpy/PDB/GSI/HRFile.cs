namespace PESpy.PDB
{
    public readonly struct HRFile
    {
        public int off => chunk.PeekInt32(0);

        public int cRef => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //off
            sizeof(int);  //cRef

        private readonly MemoryChunk chunk;

        internal HRFile(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
