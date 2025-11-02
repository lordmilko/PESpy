using System;

namespace PESpy
{
    public readonly struct AddrHash32v4 : IAddrHash32
    {
        public int Kind => 4;

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        //v4 specific

        public NativeSpan<ushort> OffsetCounts { get; }

        public NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] OffsetTable { get; }

        public int Offset { get; }

        internal AddrHash32v4(
            int offset,
            ushort cSeg,
            ushort alignment,
            NativeSpan<int> segmentTable,
            NativeSpan<ushort> offsetCounts,
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable)
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

            return TryBinarySearchSegmentOffsets16(segmentOffsets, relativeOffset, out symbolOffset);
        }

        internal static bool TryBinarySearchSegmentOffsets16(
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)> segmentOffsets,
            int relativeOffset,
            out int symbolOffset)
        {
            throw new NotImplementedException();
        }

        internal static bool TryBinarySearchSegmentOffsets32(
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)> segmentOffsets,
            int relativeOffset,
            out int symbolOffset)
        {
            //AddressMapSymTypeList.GetNearestSym has special logic for being right biased and calculating displacement.
            //We don't currently have that here

            var lo = 0;
            var hi = segmentOffsets.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                var item = segmentOffsets[mid];

                if (item.sectionRelativeOffset > relativeOffset)
                    hi = mid - 1;
                else if (item.sectionRelativeOffset < relativeOffset)
                    lo = mid + 1;
                else
                {
                    symbolOffset = item.symbolOffset;
                    return true;
                }

            }

            symbolOffset = default;
            return false;
        }
    }
}
