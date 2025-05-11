using PESpy.View;

namespace PESpy.NE
{
    //There's another type new_seg1 which has an additional field "ns_handle". I don't know when new_seg1 should be used

    //https://github.com/qb40/exe-format -> Segment Table
    //new_seg
    public readonly struct NewSeg : IValue, IViewable
    {
        /// <summary>
        /// Segment type mask
        /// </summary>
        public const ushort NSTYPE = 0x0007;

        /// <summary>
        /// Segment data aligned on 512 byte boundaries
        /// </summary>
        public const byte NSALIGN = 9;

        /// <summary>
        /// Logical-sector offset (n byte) to the contents of the segment
        /// data, relative to the beginning of the file. Zero means no file data.<para/>
        /// This value must be shifted by left <see cref="ImageOS2Header.SegmentAlignmentShiftCount"/> to get the absolute
        /// file position.
        /// </summary>
        public ushort ns_sector => chunk.PeekUInt16(0);

        /// <summary>
        /// Length of the segment in the file, in bytes. Zero means 64K.
        /// </summary>
        public ushort ns_cbseg => chunk.PeekUInt16(2);

        /// <summary>
        /// Attribute flags
        /// </summary>
        public NewSegFlags ns_flags => (NewSegFlags) chunk.PeekUInt16(4);

        /// <summary>
        /// Minimum allocation size of the segment, in bytes. Total size of the segment. Zero means 64K.
        /// </summary>
        public ushort ns_minalloc => chunk.PeekUInt16(6);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //ns_sector
            sizeof(ushort) + //ns_cbseg
            sizeof(ushort) + //ns_flags
            sizeof(ushort);  //ns_minalloc

        private readonly MemoryChunk chunk;

        internal NewSeg(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("new_seg", this, ViewKind.NewSeg);

            s.WriteField(nameof(ns_sector), ns_sector);
            s.WriteField(nameof(ns_cbseg), ns_cbseg);
            s.WriteField(nameof(ns_flags), ns_flags, sizeof(short));
            s.WriteField(nameof(ns_minalloc), ns_minalloc);
        }
    }
}
