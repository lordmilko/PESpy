using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, UTF-8 character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct Utf8String :
        IString<Utf8String, byte>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly byte* Value;

        private string DebuggerDisplay => this.ToString();

        public Utf8String(byte* value) => this.Value = value;

        public int Length => StringHelpers.GetStringLength(Value);

        #region IString

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) =>
            StringHelpers.StartsWith(AsSpan(), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(string value) =>
            StringHelpers.EndsWith(AsSpan(), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string value) =>
            StringHelpers.Contains(AsSpan(), value);

        public void CopyTo(Span<byte> destination) =>
            new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(Span<char> destination) =>
            StringHelpers.CopyTo(AsSpan(), destination);

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        #endregion
        #region IEquatable / IComparable (Utf8String)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Utf8String other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Utf8String other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareToIgnoreCase(Utf8String other) =>
            StringHelpers.CompareToIgnoreCase(AsSpan(), other.AsSpan());

        #endregion
        #region IEquatable / IComparable (string)

        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            return StringHelpers.Equals(Value, Length, other.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<char> other) => StringHelpers.Equals(Value, Length, other);

        public int CompareTo(string other)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Operators

        //Conversions

        public static implicit operator byte*(Utf8String value) => value.Value;

        public static explicit operator Utf8String(byte* value) => new Utf8String(value);

        public static explicit operator FixedUtf8String(Utf8String value) => new FixedUtf8String(value.Value, value.Length);

        //Equality

        public static bool operator ==(Utf8String left, string right) => left.Equals(right);
        public static bool operator !=(Utf8String left, string right) => !left.Equals(right);

        public static bool operator ==(string left, Utf8String right) => right.Equals(left);
        public static bool operator !=(string left, Utf8String right) => !right.Equals(left);

        public static bool operator ==(Utf8String left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(Utf8String left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator ==(ReadOnlySpan<char> left, Utf8String right) => right.Equals(left);
        public static bool operator !=(ReadOnlySpan<char> left, Utf8String right) => !right.Equals(left);

        public static bool operator ==(Utf8String left, Utf8String right) => right.Equals(left);
        public static bool operator !=(Utf8String left, Utf8String right) => !right.Equals(left);

        public static bool operator ==(Utf8String left, ReadOnlySpan<byte> right) => left.AsSpan().SequenceEqual(right);
        public static bool operator !=(Utf8String left, ReadOnlySpan<byte> right) => !left.AsSpan().SequenceEqual(right);

        #endregion

        public override bool Equals(object? obj)
        {
            if (obj is Utf8String p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            if (obj == null)
                return Value == default;

            return false;
        }

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);
    }
}
