using System;

namespace PESpy.PDB
{
    public readonly struct NI : IEquatable<NI>
    {
        public const uint Nil = 0;

        private readonly uint value;

        public NI(uint value)
        {
            this.value = value;
        }

        public static implicit operator uint(NI value) => value.value;
        public static implicit operator NI(uint value) => new NI(value);

        public static implicit operator int(NI value) => (int) value.value;
        public static implicit operator NI(int value) => new NI((uint) value);

        public bool Equals(NI other) => value == other.value;

        public override bool Equals(object? obj)
        {
            if (obj is NI o)
                return o.value == value;

            if (obj is int i)
                return (int) value == i;

            if (obj is uint u)
                return u == value;

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString()
        {
            if (value == Nil)
                return "niNil";

            return value.ToString();
        }
    }
}
