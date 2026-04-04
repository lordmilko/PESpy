using System;
using PESpy.PDB;

namespace PESpy
{
    public readonly struct AddrHash32v8 : IAddrHashInternal32
    {
        public int Kind => 8;

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        //v8 specific

        public NativeSpan<ushort> OffsetCounts { get; }

        public NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] OffsetTable { get; }

        public int GetOffsetCount(ISECT seg) => OffsetCounts[seg - 1];

        public int Offset { get; }

        internal AddrHash32v8(
            int offset,
            ushort cSeg,
            ushort alignment,
            NativeSpan<int> segmentTable,
            NativeSpan<ushort> offsetCounts,
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable)
        {
            Offset = offset;

            this.cSeg = cSeg;
            Alignment = alignment;
            SegmentTable = segmentTable;
            OffsetCounts = offsetCounts;
            OffsetTable = offsetTable;
        }

        public (int symbolOffset, int sectionRelativeOffset) this[ISECT seg, int offsetIndex] => OffsetTable[seg - 1][offsetIndex];

        public bool GetNearestSymbol(ISECT sectionNumber, int relativeOffset, out int symbolOffset) =>
            AddrHashHelpers.GetNearestSymbolInternal(this, sectionNumber, relativeOffset, out symbolOffset);

        void IAddrHashInternal32.BinarySearchAddressMap(
            int relativeOffset,
            ISECT sectionNumber,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            if (sectionNumber < 1)
            {
                AddrHashHelpers.GetFirstSymbol32(OffsetTable, out resultSeg, out resultOffsetIndex);
                return;
            }

            if (sectionNumber > cSeg)
            {
                AddrHashHelpers.GetLastSymbol32(OffsetTable, out resultSeg, out resultOffsetIndex);
                return;
            }

            var segmentOffsets = OffsetTable[sectionNumber - 1];

            if (AddrHashHelpers.TryBinarySearchSegmentOffsets32(segmentOffsets, relativeOffset, out _, out resultOffsetIndex))
            {
                resultSeg = sectionNumber;
                return;
            }

            if (!AddrHashHelpers.TryLinearSearchOffsetTable32(OffsetTable, sectionNumber, out _, out resultSeg, out resultOffsetIndex))
                throw new NotImplementedException("Don't know how to handle a linear search failing");
        }
    }
}
