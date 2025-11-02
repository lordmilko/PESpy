namespace PESpy
{
    public interface IAddrHash32 : IValue
    {
        public int Kind { get; }

        public ushort cSeg { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> SegmentTable { get; }

        //What follows is variable depending on the version of the address sort table

        bool TryGetSymbolOffset(ushort sectionNumber, int relativeOffset, out int symbolOffset);
    }
}
