using ClrDebug.OMF;

namespace PESpy
{
    public readonly struct DirEntry
    {
        public SST SubSectionType => (SST) chunk.PeekUInt16(0);

        public ushort ModuleIndex => chunk.PeekUInt16(2);

        public int lfoStart => chunk.PeekInt32(4);

        public ushort Size => chunk.PeekUInt16(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //ModuleIndex
            sizeof(int) + //lfoStart
            sizeof(ushort); //Size

        private readonly MemoryChunk chunk;

        internal DirEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return SubSectionType.ToString();
        }
    }
}
