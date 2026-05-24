using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Roslyn.Utilities;

namespace PESpy
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct FixedAnsiString :
        IString<FixedAnsiString, byte>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly byte* Value;
        public int Length { get; }

        private string DebuggerDisplay => this.ToString();

        public FixedAnsiString(byte* value, int length)
        {
            Value = value;
            Length = length;
        }

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

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(Span<char> destination) => StringHelpers.CopyTo(AsSpan(), destination);

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        #endregion
        #region IEquatable / IComparable (FixedAnsiString)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FixedAnsiString other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FixedAnsiString other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareToIgnoreCase(FixedAnsiString other) =>
            StringHelpers.CompareToIgnoreCase(AsSpan(), other.AsSpan());

        #endregion
        #region IEquatable / IComparable (string)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public static explicit operator FixedUtf8String(FixedAnsiString value) => new FixedUtf8String(value.Value, value.Length);

        public static implicit operator Span<byte>(FixedAnsiString value) => new Span<byte>(value.Value, value.Length);

        //Equality

        public static bool operator ==(FixedAnsiString left, string right) => left.Equals(right);
        public static bool operator !=(FixedAnsiString left, string right) => !left.Equals(right);

        public static bool operator ==(string left, FixedAnsiString right) => right.Equals(left);
        public static bool operator !=(string left, FixedAnsiString right) => !right.Equals(left);

        public static bool operator ==(FixedAnsiString left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(FixedAnsiString left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator ==(ReadOnlySpan<char> left, FixedAnsiString right) => right.Equals(left);
        public static bool operator !=(ReadOnlySpan<char> left, FixedAnsiString right) => !right.Equals(left);

        public static bool operator ==(FixedAnsiString left, FixedAnsiString right) => right.Equals(left);
        public static bool operator !=(FixedAnsiString left, FixedAnsiString right) => !right.Equals(left);

        public static bool operator ==(FixedAnsiString left, ReadOnlySpan<byte> right) => left.AsSpan().SequenceEqual(right);
        public static bool operator !=(FixedAnsiString left, ReadOnlySpan<byte> right) => !left.AsSpan().SequenceEqual(right);

        #endregion

        public override bool Equals(object? obj)
        {
            if (obj is FixedAnsiString p)
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
        public override string ToString() => Value is null ? null! : new string((sbyte*) Value, 0, Length, Encoding.Default);
    }
}
