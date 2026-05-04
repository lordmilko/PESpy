using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Information describing each segment in a module (16-bit / non-<see cref="LEFile"/>)<para/>
    /// "nsg" in the Microsoft C 6.0 Developer's Toolkit Reference; "oldnsg" in cvexefmt.h
    /// </summary>
    [Source(SourceKind.C6DevToolkit | SourceKind.cvexefmt_h)]
    public readonly struct nsg : IViewableValue
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
        public ushort Off => chunk.PeekUInt16(OffOffset);

        /// <summary>
        /// Number of bytes in segment
        /// </summary>
        public ushort cbSeg => chunk.PeekUInt16(cbSegOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Seg
            sizeof(short) + //Off
            sizeof(short); //cbSeg

        private readonly MemoryChunk chunk;

        internal nsg(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.nsg, StructSize);

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
