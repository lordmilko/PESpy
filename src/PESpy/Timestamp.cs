using System;
using System.Diagnostics;

namespace PESpy
{
    //Note that if a file was compiled as reproducible, the TimeDateStamp may be represent a hash rather than an actual time
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>
    {
        private readonly uint value;

        public Timestamp(uint value)
        {
            this.value = value;
        }

        public static implicit operator Timestamp(uint value) => new Timestamp(value);

        public static implicit operator uint(Timestamp value) => value.value;

        public static implicit operator DateTime(Timestamp value) => DateTimeOffset.FromUnixTimeSeconds(value.value).LocalDateTime;

        public bool Equals(Timestamp other) => other.value == value;

        public override bool Equals(object? obj)
        {
            if (obj is uint u)
                return value == u;

            if (obj is Timestamp t)
                return t.value == value;

            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public int CompareTo(Timestamp other) => value.CompareTo(other.value);

        public override string ToString()
        {
            return ((DateTime) this).ToString();
        }
    }
}
