using System;

namespace PESpy.Ecma335
{
    public readonly struct GuidIndex
    {
        public readonly int Offset;

        private readonly Func<GuidHeap?> getGuidHeap;

        internal GuidIndex(int offset, Func<GuidHeap?> getGuidHeap)
        {
            Offset = offset;
            this.getGuidHeap = getGuidHeap;
        }

        public static explicit operator GuidIndex(int value) => new GuidIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(GuidIndex value) => value.Offset;

        public override string ToString()
        {
            var guidHeap = getGuidHeap();

            if (guidHeap != null)
                return guidHeap[this].Value.ToString();

            return Offset.ToString();
        }
    }
}
