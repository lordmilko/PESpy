using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, UTF-8 character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct Utf8String : IEquatable<Utf8String>, IEquatable<string>
    {
        public readonly byte* Value;

        public Utf8String(byte* value) => this.Value = value;

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public static implicit operator byte*(Utf8String value) => value.Value;

        public static explicit operator Utf8String(byte* value) => new Utf8String(value);

        public static explicit operator FixedUtf8String(Utf8String value) => new FixedUtf8String(value.Value, value.Length);

        public bool Equals(Utf8String other)
        {
            if (Value == other.Value)
                return true;

            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public bool Equals(string? other)
        {
            if (other == null)
                return false;

            return StringHelpers.Equals(Value, other);
        }

        public static bool operator ==(Utf8String left, string right) => left.Equals(right);
        public static bool operator !=(Utf8String left, string right) => !left.Equals(right);

        public static bool operator ==(string left, Utf8String right) => right.Equals(left);
        public static bool operator !=(string left, Utf8String right) => !right.Equals(left);

        public override bool Equals(object? obj)
        {
            if (obj is Utf8String p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        public int Length => StringHelpers.GetStringLength(Value);

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);

        private string DebuggerDisplay => this.ToString();
    }
}
