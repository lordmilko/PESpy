namespace PESpy
{
    public readonly struct AddrHash32v12 : IAddrHash32
    {
        public int Kind => 12;

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        public NativeSpan<int> OffsetCounts { get; }

        public NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] OffsetTable { get; }

        public int Offset { get; }

        internal AddrHash32v12(
            int offset,
            ushort cSeg,
            ushort alignment,
            NativeSpan<int> segmentTable,
            NativeSpan<int> offsetCounts,
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable)
        {
            Offset = offset;

            this.cSeg = cSeg;
            Alignment = alignment;
            SegmentTable = segmentTable;
            OffsetCounts = offsetCounts;
            OffsetTable = offsetTable;
        }

        public bool TryGetSymbolOffset(ushort sectionNumber, int relativeOffset, out int symbolOffset)
        {
            if (sectionNumber > cSeg)
            {
                symbolOffset = default;
                return false;
            }

            var segmentOffsets = OffsetTable[sectionNumber - 1];

            return AddrHash32v4.TryBinarySearchSegmentOffsets32(segmentOffsets, relativeOffset, out symbolOffset);
        }
    }
}
