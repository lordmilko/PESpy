using System;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents a 16-bit section index.
    /// </summary>
    public readonly struct ISECT : IEquatable<ISECT>, IComparable<ISECT>
    {
        public const ushort Nil = ushort.MaxValue; //-1

        private readonly ushort value;

        public ISECT(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ushort(ISECT value) => value.value;
        public static implicit operator ISECT(ushort value) => new ISECT(value);
        public static implicit operator ISECT(int value) => new ISECT((ushort) value);

        public override bool Equals(object obj)
        {
            if (obj is ISECT s)
                return value == s.value;

            if (obj is ushort u)
                return value == u;

            if (obj is int i)
                return value == i;

            return false;
        }

        public bool Equals(ISECT other) => value == other.value;

        public int CompareTo(ISECT other) => value.CompareTo(other.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString()
        {
            return value switch
            {
                Nil => "isectNil",
                _ => value.ToString()
            };
        }
    }
}
