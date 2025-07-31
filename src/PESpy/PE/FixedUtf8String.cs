using System;

namespace PESpy
{
    public readonly unsafe struct FixedUtf8String
    {
        public readonly byte* Value;
        public readonly int Length;

        public FixedUtf8String(byte* value, int length)
        {
            Value = value;
            Length = length;
        }

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(char[] array)
        {
            var value = Value;

            for (var i = 0; i < Length; i++)
                array[i] = (char) value[i];
        }

        public bool Equals(FixedUtf8String other) => this.Value == other.Value;

        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            var length = Length;

            if (other.Length != length)
                return false;

            fixed (char* p = other)
            {
                for (var i = 0; i < length; i++)
                {
                    if ((byte) p[i] != Value[i])
                        return false;
                }
            }

            return true;
        }

        public static bool operator ==(FixedUtf8String left, string? right) => left.Equals(right);
        public static bool operator !=(FixedUtf8String left, string? right) => !left.Equals(right);

        public static bool operator ==(string? left, FixedUtf8String right) => right.Equals(left);
        public static bool operator !=(string? left, FixedUtf8String right) => !right.Equals(left);

        public static bool operator ==(FixedUtf8String left, FixedUtf8String right) => Equals(left, right);
        public static bool operator !=(FixedUtf8String left, FixedUtf8String right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (obj is FixedUtf8String p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public bool StartsWith(string value)
        {
            if (value.Length > Length)
                return false;

            //We currently only support ANSI values

            for (var i = 0; i < value.Length; i++)
            {
                if ((byte) value[i] != Value[i])
                    return false;
            }

            return true;
        }

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        public override int GetHashCode() => unchecked((int) this.Value);

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);

        private string DebuggerDisplay => this.ToString();
    }
}
