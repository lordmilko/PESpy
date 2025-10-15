using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public readonly struct OMFSegDesc : IValue, IViewable
    {
        private const int SegOffset = 0;
        private const int padOffset = 2;
        private const int OffOffset = 4;
        private const int cbSegOffset = 8;

        public ushort Seg => chunk.PeekUInt16(SegOffset);

        public ushort pad => chunk.PeekUInt16(padOffset);

        public int Off => chunk.PeekInt32(OffOffset);

        public int cbSeg => chunk.PeekInt32(cbSegOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //Seg
            sizeof(ushort) + //pad
            sizeof(int) +    //Off
            sizeof(int);     //cbSeg

        private readonly MemoryChunk chunk;

        internal OMFSegDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFModule, this, ViewKind.OMFSegDesc, StructSize);

        int IViewable.NumChildren => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Seg), SegOffset, Seg);
                    break;

                case 1:
                    structWriter.WriteField(nameof(pad), padOffset, pad);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Off), OffOffset, Off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbSeg), cbSegOffset, cbSeg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
