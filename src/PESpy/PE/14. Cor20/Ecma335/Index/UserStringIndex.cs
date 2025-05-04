namespace PESpy.Ecma335
{
    public readonly struct UserStringIndex
    {
        public readonly int Offset;

        private readonly UserStringHeap? userStringHeap;

        internal UserStringIndex(int offset, UserStringHeap? userStringHeap)
        {
            Offset = offset;
            this.userStringHeap = userStringHeap;
        }

        public static explicit operator UserStringIndex(int value) => new UserStringIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(UserStringIndex value) => value.Offset;

        public override string ToString()
        {
            if (userStringHeap != null)
                return $"\"{userStringHeap.GetString(Offset)}\"";

            return Offset.ToString();
        }
    }
}
