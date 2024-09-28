namespace PESpy
{
#if DEBUG_POSITION
    [DebuggerDisplay("{Value}")]
    public readonly struct RVA
    {
        public int Value { get; }

        public static RVA operator +(RVA left, RVA right) => new RVA(left.Value + right.Value);
        public static RVA operator +(RVA left, int right) => new RVA(left.Value + right);
        public static RVA operator +(int left, RVA right) => new RVA(left + right.Value);
        public static RVA operator -(RVA left, RVA right) => new RVA(left.Value - right.Value);
        public static RVA operator -(RVA left, int right) => new RVA(left.Value - right);
        public static RVA operator -(int left, RVA right) => new RVA(left - right.Value);

        public static bool operator >(RVA left, RVA right) => left.Value > right.Value;
        public static bool operator >(RVA left, int right) => left.Value > right;
        public static bool operator <(RVA left, RVA right) => left.Value < right.Value;
        public static bool operator <(RVA left, int right) => left.Value < right;
        public static bool operator ==(RVA left, RVA right) => left.Value == right.Value;
        public static bool operator ==(RVA left, int right) => left.Value == right;
        public static bool operator !=(RVA left, RVA right) => left.Value != right.Value;
        public static bool operator !=(RVA left, int right) => left.Value != right;
        public static bool operator >=(RVA left, RVA right) => left.Value >= right.Value;
        public static bool operator >=(RVA left, int right) => left.Value >= right;
        public static bool operator <=(RVA left, RVA right) => left.Value <= right.Value;
        public static bool operator <=(RVA left, int right) => left.Value <= right;

        public static explicit operator RawOffset(RVA value) => new RawOffset(value.Value);
        public static explicit operator RVA(int value) => new RVA(value);
        public static explicit operator int(RVA value) => value.Value;

        public RVA(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RVA v)
                return Value == v.Value;

            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public string ToString(string format) => Value.ToString(format);
    }

    [DebuggerDisplay("{Value}")]
    public readonly struct RawOffset
    {
        public int Value { get; }

        public static RawOffset operator +(RawOffset left, RawOffset right) => new RawOffset(left.Value + right.Value);
        public static RawOffset operator +(RawOffset left, int right) => new RawOffset(left.Value + right);
        public static RawOffset operator +(int left, RawOffset right) => new RawOffset(left + right.Value);
        public static RawOffset operator -(RawOffset left, RawOffset right) => new RawOffset(left.Value - right.Value);
        public static RawOffset operator -(RawOffset left, int right) => new RawOffset(left.Value - right);
        public static RawOffset operator -(int left, RawOffset right) => new RawOffset(left - right.Value);

        public static RawOffset operator ++(RawOffset value) => new RawOffset(value.Value + 1);

        public static bool operator >(RawOffset left, RawOffset right) => left.Value > right.Value;
        public static bool operator >(RawOffset left, int right) => left.Value > right;
        public static bool operator <(RawOffset left, RawOffset right) => left.Value < right.Value;
        public static bool operator <(RawOffset left, int right) => left.Value < right;
        public static bool operator ==(RawOffset left, RawOffset right) => left.Value == right.Value;
        public static bool operator ==(RawOffset left, int right) => left.Value == right;
        public static bool operator !=(RawOffset left, RawOffset right) => left.Value != right.Value;
        public static bool operator !=(RawOffset left, int right) => left.Value != right;
        public static bool operator >=(RawOffset left, RawOffset right) => left.Value >= right.Value;
        public static bool operator >=(RawOffset left, int right) => left.Value >= right;
        public static bool operator <=(RawOffset left, RawOffset right) => left.Value <= right.Value;
        public static bool operator <=(RawOffset left, int right) => left.Value <= right;

        public static explicit operator RVA(RawOffset value) => new RVA(value.Value);
        public static explicit operator RawOffset(int value) => new RawOffset(value);
        public static explicit operator int(RawOffset value) => value.Value;

        public RawOffset(int value)
        {
            Value = value;
        }

        public int CompareTo(RawOffset other) => Value.CompareTo(other.Value);

        public override bool Equals(object obj)
        {
            if (obj is RawOffset v)
                return Value == v.Value;

            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public string ToString(string format) => Value.ToString(format);
    }
#endif
}
