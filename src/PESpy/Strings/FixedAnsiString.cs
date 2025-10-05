using System;
using Roslyn.Utilities;

namespace PESpy
{
    public readonly unsafe struct FixedAnsiString
    {
        public readonly byte* Value;
        public readonly int Length;

        public FixedAnsiString(byte* value, int length)
        {
            Value = value;
            Length = length;
        }

        public static explicit operator FixedUtf8String(FixedAnsiString value) => new FixedUtf8String(value.Value, value.Length);

        public bool Equals(FixedAnsiString other)
        {
            if (Value == other.Value)
                return true;

            return AsSpan().SequenceEqual(other.AsSpan());
        }

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

        public static implicit operator Span<byte>(FixedAnsiString value) => new Span<byte>(value.Value, value.Length);

        public static bool operator ==(FixedAnsiString left, string right) => left.Equals(right);
        public static bool operator !=(FixedAnsiString left, string right) => !left.Equals(right);

        public static bool operator ==(string left, FixedAnsiString right) => right.Equals(left);
        public static bool operator !=(string left, FixedAnsiString right) => !right.Equals(left);

        public override bool Equals(object? obj)
        {
            if (obj is FixedAnsiString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, global::System.Text.Encoding.Default);

        private string DebuggerDisplay => this.ToString();
    }
}
