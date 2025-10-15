using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //NT 4 refers to this as the CV 4.0 dnthdr/DNTHDR
    [Source(SourceKind.cvexefmt)]
    public readonly struct OMFDirHeader : IValue, IViewable
    {
        private const int cbDirHeaderOffset = 0;
        private const int cbDirEntryOffset = 2;
        private const int cDirOffset = 4;
        private const int lfoNextDirOffset = 8;
        private const int flagsOffset = 12;

        public ushort cbDirHeader => chunk.PeekUInt16(cbDirHeaderOffset);

        public ushort cbDirEntry => chunk.PeekUInt16(cbDirEntryOffset);

        public int cDir => chunk.PeekInt32(cDirOffset);

        public int lfoNextDir => chunk.PeekInt32(lfoNextDirOffset);

        public int flags => chunk.PeekInt32(flagsOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //cbDirHeader
            sizeof(ushort) + //cbDirEntry
            sizeof(int) + //cDir
            sizeof(int) + //lfoNextDir
            sizeof(int); //flags

        private readonly MemoryChunk chunk;

        internal OMFDirHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFDirHeader, this, ViewKind.OMFDirHeader, StructSize);

        int IViewable.NumChildren => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cbDirHeader), cbDirHeaderOffset, cbDirHeader);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cbDirEntry), cbDirEntryOffset, cbDirEntry);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cDir), cDirOffset, cDir);
                    break;

                case 3:
                    structWriter.WriteField(nameof(lfoNextDir), lfoNextDirOffset, lfoNextDir);
                    break;

                case 4:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
