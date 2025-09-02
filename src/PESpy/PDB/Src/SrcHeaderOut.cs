using ClrDebug.PDB;

namespace PESpy
{
    public readonly struct SrcHeaderOut
    {
        public int cb => chunk.PeekInt32(0);

        public int ver => chunk.PeekInt32(4);

        public uint sig => chunk.PeekUInt32(8);

        public int cbSource => chunk.PeekInt32(12);

        public int niFile => chunk.PeekInt32(16);

        public int niObj => chunk.PeekInt32(24);

        public int niVirt => chunk.PeekInt32(24);

        public SrcCompress srccompress => (SrcCompress) chunk.PeekByte(28);

        public byte grFlags => chunk.PeekByte(29);

        public short sPad => chunk.PeekInt16(30);

        public long pv64Reserved2 => chunk.PeekInt64(32);

        public int Offset => chunk.AbsoluteOffset;

        public AnsiString FileName => chunk.PDBFile().NameMap?.GetStringFromNI(niFile) ?? default;

        public AnsiString ObjectFileName => chunk.PDBFile().NameMap?.GetStringFromNI(niObj) ?? default;

        public AnsiString VirtualFileName => chunk.PDBFile().NameMap?.GetStringFromNI(niFile) ?? default;

        internal const int StructSize =
            sizeof(int) + //cb
            sizeof(int) + //ver
            sizeof(int) + //sig
            sizeof(int) + //cbSource
            sizeof(int) + //niFile
            sizeof(int) + //niObj
            sizeof(int) + //niVirt
            sizeof(byte) + //srccompress
            sizeof(byte) + //grFlags
            sizeof(short) + //spad
            sizeof(long); //pv64Reserved2;

        private readonly MemoryChunk chunk;

        internal SrcHeaderOut(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return FileName.ToString();
        }
    }
}
