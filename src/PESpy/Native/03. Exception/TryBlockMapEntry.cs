namespace PESpy.Native
{
    internal struct TryBlockMapEntry
    {
        public int tryLow;
        public int tryHigh;
        public int catchHigh;
        public int nCatches;
        public int dispHandlerArray;
    }
}
