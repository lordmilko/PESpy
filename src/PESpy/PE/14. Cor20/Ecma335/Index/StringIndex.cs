using System;

namespace PESpy.Ecma335
{
    public readonly struct StringIndex
    {
        public readonly int Offset;

        private readonly Func<StringHeap?> getStringHeap;

        internal StringIndex(int offset, Func<StringHeap?> getStringHeap)
        {
            Offset = offset;
            this.getStringHeap = getStringHeap;
        }

        public static explicit operator StringIndex(int value) => new StringIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(StringIndex value) => value.Offset;

        public override string ToString()
        {
            var stringHeap = getStringHeap();

            if (stringHeap != null)
                return $"\"{stringHeap.GetString(Offset)}\"";

            return Offset.ToString();
        }
    }
}
