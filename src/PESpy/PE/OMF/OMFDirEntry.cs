using ClrDebug.PDB;

namespace PESpy
{
    public readonly struct OMFDirEntry
    {
        public NB05Subsection SubSection => (NB05Subsection) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(int); //cb

        private readonly MemoryChunk chunk;

        internal OMFDirEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
