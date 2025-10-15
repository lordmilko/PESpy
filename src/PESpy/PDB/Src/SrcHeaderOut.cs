using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy
{
    public readonly struct SrcHeaderOut : IValue, IViewable
    {
        private const int cbOffset = 0;
        private const int verOffset = 4;
        private const int sigOffset = 8;
        private const int cbSourceOffset = 12;
        private const int niFileOffset = 16;
        private const int niObjOffset = 24;
        private const int niVirtOffset = 24;
        private const int srccompressOffset = 28;
        private const int grFlagsOffset = 29;
        private const int sPadOffset = 30;
        private const int pv64Reserved2Offset = 32;

        public int cb => chunk.PeekInt32(cbOffset);

        public int ver => chunk.PeekInt32(verOffset);

        public uint sig => chunk.PeekUInt32(sigOffset);

        public int cbSource => chunk.PeekInt32(cbSourceOffset);

        public int niFile => chunk.PeekInt32(niFileOffset);

        public int niObj => chunk.PeekInt32(niObjOffset);

        public int niVirt => chunk.PeekInt32(niVirtOffset);

        public SrcCompress srccompress => (SrcCompress) chunk.PeekByte(srccompressOffset);

        public byte grFlags => chunk.PeekByte(grFlagsOffset);

        public short sPad => chunk.PeekInt16(sPadOffset);

        public long pv64Reserved2 => chunk.PeekInt64(pv64Reserved2Offset);

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
