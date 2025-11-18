using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy
{
    public readonly struct SrcHeaderOut : IViewableValue
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.SrcHeaderOut, this, ViewKind.SrcHeaderOut, StructSize);

        int IViewable.NumChildren() => 11;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ver), verOffset, ver);
                    break;

                case 2:
                    structWriter.WriteField(nameof(sig), sigOffset, sig);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbSource), cbSourceOffset, cbSource);
                    break;

                case 4:
                    structWriter.WriteField(nameof(niFile), niFileOffset, niFile);
                    break;

                case 5:
                    structWriter.WriteField(nameof(niObj), niObjOffset, niObj);
                    break;

                case 6:
                    structWriter.WriteField(nameof(niVirt), niVirtOffset, niVirt);
                    break;

                case 7:
                    structWriter.WriteField(nameof(srccompress), srccompressOffset, srccompress, sizeof(byte));
                    break;

                case 8:
                    structWriter.WriteField(nameof(grFlags), grFlagsOffset, grFlags);
                    break;

                case 9:
                    structWriter.WriteField(nameof(sPad), sPadOffset, sPad);
                    break;

                case 10:
                    structWriter.WriteField(nameof(pv64Reserved2), pv64Reserved2Offset, pv64Reserved2);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
