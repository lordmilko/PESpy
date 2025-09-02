using System;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents a 16-bit module index.
    /// </summary>
    public readonly struct IMOD : IEquatable<IMOD>, IComparable<IMOD>
    {
        public const ushort Nil = ushort.MaxValue; //-1

        private readonly ushort value;

        public IMOD(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ushort(IMOD value) => value.value;
        public static implicit operator IMOD(ushort value) => new IMOD(value);

        public override bool Equals(object obj)
        {
            if (obj is IMOD s)
                return value == s.value;

            if (obj is ushort u)
                return value == u;

            if (obj is int i)
                return value == i;

            return false;
        }

        public bool Equals(IMOD other) => value == other.value;

        public int CompareTo(IMOD other) => value.CompareTo(other.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString()
        {
            return value switch
            {
                Nil => "imodNil",
                _ => value.ToString()
            };
        }
    }
}
