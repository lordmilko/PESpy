using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Information describing each segment in a module
    /// </summary>
    public readonly struct nsg32 : IValue, IViewable
    {
        private const int SegOffset = 0;
        private const int OffOffset = 2;
        private const int cbSegOffset = 4;

        /// <summary>
        /// Segment index
        /// </summary>
        public ushort Seg => chunk.PeekUInt16(SegOffset);

        /// <summary>
        /// Offset of code in segment
        /// </summary>
        public int Off => chunk.PeekUInt16(OffOffset);

        /// <summary>
        /// Number of bytes in segment
        /// </summary>
        public int cbSeg => chunk.PeekUInt16(cbSegOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Seg
            sizeof(int) +   //Off
            sizeof(int);    //cbSeg

        private readonly MemoryChunk chunk;

        internal nsg32(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.nsg32, this, ViewKind.nsg32, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Seg), SegOffset, Seg);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Off), OffOffset, Off);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cbSeg), cbSegOffset, cbSeg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
