namespace PESpy.Ecma335
{
    public readonly struct ArrayShape
    {
        public int Rank { get; }

        public int[] Sizes { get; }

        public int[] LowerBounds { get; }

        internal ArrayShape(int rank, int[] sizes, int[] lowerBounds)
        {
            Rank = rank;
            Sizes = sizes;
            LowerBounds = lowerBounds;
        }
    }
}
