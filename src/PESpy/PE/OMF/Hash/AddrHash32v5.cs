namespace PESpy
{
    public readonly struct AddrHash32v5 : IAddrHash32
    {
        public int Kind => 5;

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        //v5 specific

        public NativeSpan<ushort> OffsetCounts { get; }

        //If cSeg is odd, there's padding here
        public ushort Padding { get; }

        public NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] OffsetTable { get; }

        public int Offset { get; }

        internal AddrHash32v5(
            int offset,
            ushort cSeg,
            ushort alignment,
            NativeSpan<int> segmentTable,
            NativeSpan<ushort> offsetCounts,
            ushort padding,
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable)
        {
            Offset = offset;

            this.cSeg = cSeg;
            Alignment = alignment;
            SegmentTable = segmentTable;
            OffsetCounts = offsetCounts;
            Padding = padding;
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

            return AddrHash32v4.TryBinarySearchSegmentOffsets16(segmentOffsets, relativeOffset, out symbolOffset);
        }
    }
}
