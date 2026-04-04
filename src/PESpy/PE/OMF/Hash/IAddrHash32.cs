using PESpy.PDB;

namespace PESpy
{
    internal interface IAddrHashInternal32 : IAddrHash32
    {
        void BinarySearchAddressMap(
            int relativeOffset,
            ISECT sectionNumber,
            out ISECT resultSeg,
            out int resultOffsetIndex);
    }

    public interface IAddrHash32 : IValue
    {
        public int Kind { get; }

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        //What follows is variable depending on the version of the address sort table

        int GetOffsetCount(ISECT seg);

        (int symbolOffset, int sectionRelativeOffset) this[ISECT seg, int index] { get; }

        bool GetNearestSymbol(ISECT sectionNumber, int relativeOffset, out int symbolOffset);
    }
}
