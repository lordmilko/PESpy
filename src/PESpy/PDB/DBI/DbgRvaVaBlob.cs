using System.Diagnostics;

namespace PESpy.PDB
{
    public readonly struct DbgRvaVaBlob
    {
        public int ver => chunk.PeekInt32(0);

        public int cbHdr => chunk.PeekInt32(4);

        public int cbData => chunk.PeekInt32(8);

        public int rvaDataBase => chunk.PeekInt32(12);

        public long vaImageBase => chunk.PeekInt64(16);

        public int ulReserved1 => chunk.PeekInt32(24);

        public int ulReserved2 => chunk.PeekInt32(28);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ver
            sizeof(int) + //cbHdr
            sizeof(int) + //cbData
            sizeof(int) + //rvaDataBase
            sizeof(long) + //vaImageBase
            sizeof(int) + //ulReserved1
            sizeof(int); //ulReserved2

        private readonly MemoryChunk chunk;

        internal DbgRvaVaBlob(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            Debug.Assert(cbHdr == StructSize); //The fixed fields combined total 32 bytes. If this says the header is anything but that, we're in trouble
        }
    }
}
