using System;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    public readonly struct AddrHash32v5 : IAddrHashInternal32
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

        public long Offset { get; }

        private int StructSize
        {
            get
            {
                var size =
                    sizeof(short) + //cSeg
                    sizeof(short); //pad

                size += (cSeg * (sizeof(int) + sizeof(short))); //SegmentTable, OffsetCounts

                if ((cSeg & 1) != 0)
                    size += sizeof(short); //Padding

                //OffsetTable
                foreach (var count in OffsetCounts)
                    size += (count * (sizeof(short) + sizeof(short)));

                return size;
            }
        }

        internal AddrHash32v5(
            long offset,
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

        public (int symbolOffset, int sectionRelativeOffset) this[ISECT seg, int offsetIndex] => OffsetTable[seg - 1][offsetIndex];

        public int GetOffsetCount(ISECT seg) => OffsetCounts[seg - 1];

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
                AddrHashHelpers.GetFirstSymbol16(OffsetTable, out resultSeg, out resultOffsetIndex);
                return;
            }

            if (sectionNumber > cSeg)
            {
                AddrHashHelpers.GetLastSymbol16(OffsetTable, out resultSeg, out resultOffsetIndex);
                return;
            }

            var segmentOffsets = OffsetTable[sectionNumber - 1];

            if (AddrHashHelpers.TryBinarySearchSegmentOffsets16(segmentOffsets, relativeOffset, out _, out resultOffsetIndex))
            {
                resultSeg = sectionNumber;
                return;
            }

            if (!AddrHashHelpers.TryLinearSearchOffsetTable16(OffsetTable, sectionNumber, out _, out resultSeg, out resultOffsetIndex))
                throw new NotImplementedException("Don't know how to handle a linear search failing");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.AddrHash32v5, StructSize);

        int IViewable.NumChildren() => (cSeg & 1) != 0 ? 6 : 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cSeg), 0, cSeg);
                    break;

                case 1:
                    //Name is made up
                    structWriter.WriteField("pad", 2, Alignment);
                    break;

                case 2:
                    structWriter.WriteField("rgulSeg", 4, SegmentTable);
                    break;

                case 3:
                    structWriter.WriteField("rglCSeg", 4 + (SegmentTable.Length * sizeof(int)), OffsetCounts);
                    break;

                case 4:
                case 5:
                    //If we need padding, this will in fact be the offset of the padding
                    var offsetTableOffset = 4 + (SegmentTable.Length * (sizeof(int) + sizeof(short)));

                    if ((cSeg & 1) != 0)
                    {
                        if (index == 4)
                        {
                            structWriter.WriteField(nameof(Padding), offsetTableOffset, Padding);
                            return;
                        }

                        offsetTableOffset += sizeof(short);
                    }
                    else
                    {
                        if (index == 5)
                            throw new IndexOutOfRangeException();
                    }

                    //This isn't the right thing to do, but it'll do for now
                    var offsetTableSize = 0;

                    foreach (var count in OffsetCounts)
                        offsetTableSize += (count * (sizeof(short) + sizeof(short)));

                    structWriter.WriteField(nameof(OffsetTable), offsetTableOffset, OffsetTable, offsetTableSize);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
