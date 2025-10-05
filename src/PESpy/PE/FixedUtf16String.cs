using System;
using Roslyn.Utilities;

namespace PESpy
{
    public readonly unsafe struct FixedUtf16String
    {
        public readonly char* Value;
        public readonly int Length;

        public FixedUtf16String(char* value, int length)
        {
            Value = value;
            Length = length;
        }

        public void CopyTo(Span<char> destination) => new Span<char>(Value, Length).CopyTo(destination);

        public bool Equals(FixedUtf16String other)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) => AsSpan().StartsWith(value.AsSpan());

        public static bool operator ==(FixedUtf16String left, string right) => left.Equals(right);
        public static bool operator !=(FixedUtf16String left, string right) => !left.Equals(right);

        public static bool operator ==(string left, FixedUtf16String right) => right.Equals(left);
        public static bool operator !=(string left, FixedUtf16String right) => !right.Equals(left);

        public override bool Equals(object obj)
        {
            if (obj is AnsiString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public Span<char> AsSpan() => new Span<char>(Value, Length);

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string(this.Value, 0, Length);

        private string DebuggerDisplay => this.ToString();
    }
}
