using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.NE
{
    //There's another type new_seg1 which has an additional field "ns_handle". I don't know when new_seg1 should be used

    //https://github.com/qb40/exe-format -> Segment Table
    //new_seg
    public readonly struct new_seg : IValue, IViewable
    {
        /// <summary>
        /// Segment type mask
        /// </summary>
        public const ushort NSTYPE = 0x0007;

        /// <summary>
        /// Segment data aligned on 512 byte boundaries
        /// </summary>
        public const byte NSALIGN = 9;

        private const int ns_sectorOffset = 0;
        private const int ns_cbsegOffset = 2;
        private const int ns_flagsOffset = 4;
        private const int ns_minallocOffset = 6;

        /// <summary>
        /// Logical-sector offset (n byte) to the contents of the segment
        /// data, relative to the beginning of the file. Zero means no file data.<para/>
        /// This value must be shifted by left <see cref="ImageOS2Header.ne_align"/> to get the absolute
        /// file position.
        /// </summary>
        public ushort ns_sector => chunk.PeekUInt16(ns_sectorOffset);

        /// <summary>
        /// Length of the segment in the file, in bytes. Zero means 64K.
        /// </summary>
        public ushort ns_cbseg => chunk.PeekUInt16(ns_cbsegOffset);

        /// <summary>
        /// Attribute flags
        /// </summary>
        public NewSegFlags ns_flags => (NewSegFlags) chunk.PeekUInt16(ns_flagsOffset);

        /// <summary>
        /// Minimum allocation size of the segment, in bytes. Total size of the segment. Zero means 64K.
        /// </summary>
        public ushort ns_minalloc => chunk.PeekUInt16(ns_minallocOffset);

        public unsafe NativeSpan<byte> Bytes
        {
            get
            {
                var neFile = chunk.NEFile();

                var segmentStart = ns_sector << neFile.OS2Header.ne_align;
                var segmentLength = ns_cbseg;

                return new NativeSpan<byte>(chunk.block.LocalPointer + segmentStart, segmentLength);
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //ns_sector
            sizeof(ushort) + //ns_cbseg
            sizeof(ushort) + //ns_flags
            sizeof(ushort);  //ns_minalloc

        private readonly MemoryChunk chunk;

        internal new_seg(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.NewSeg, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ns_sector), ns_sectorOffset, ns_sector);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ns_cbseg), ns_cbsegOffset, ns_cbseg);
                    break;

                case 2:
                    structWriter.WriteField(nameof(ns_flags), ns_flagsOffset, ns_flags, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteField(nameof(ns_minalloc), ns_minallocOffset, ns_minalloc);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
