using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace PESpy
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct FixedUtf8String :
        IString<FixedUtf8String, byte>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly byte* Value;
        public int Length { get; }

        private string DebuggerDisplay => this.ToString();

        public FixedUtf8String(byte* value, int length)
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
        #region IEquatable / IComparable (FixedUtf8String)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FixedUtf8String other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FixedUtf8String other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareToIgnoreCase(FixedUtf8String other) =>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(string other) =>
            StringHelpers.CompareTo(AsSpan(), other);

        #endregion
        #region Operators

        //Conversions

        public static implicit operator Span<byte>(FixedUtf8String value) => new Span<byte>(value.Value, value.Length);

        //Equality

        public static bool operator ==(FixedUtf8String left, string? right) => left.Equals(right);
        public static bool operator !=(FixedUtf8String left, string? right) => !left.Equals(right);

        public static bool operator ==(string? left, FixedUtf8String right) => right.Equals(left);
        public static bool operator !=(string? left, FixedUtf8String right) => !right.Equals(left);

        public static bool operator ==(FixedUtf8String left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(FixedUtf8String left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator ==(ReadOnlySpan<char> left, FixedUtf8String right) => right.Equals(left);
        public static bool operator !=(ReadOnlySpan<char> left, FixedUtf8String right) => !right.Equals(left);

        public static bool operator ==(FixedUtf8String left, FixedUtf8String right) => Equals(left, right);
        public static bool operator !=(FixedUtf8String left, FixedUtf8String right) => !Equals(left, right);

        public static bool operator ==(FixedUtf8String left, ReadOnlySpan<byte> right) => left.AsSpan().SequenceEqual(right);
        public static bool operator !=(FixedUtf8String left, ReadOnlySpan<byte> right) => !left.AsSpan().SequenceEqual(right);

        #endregion

        public FixedUtf8String Slice(int start) => new FixedUtf8String(Value + start, Length - start);

        public FixedUtf8String Slice(int start, int length) => new FixedUtf8String(Value + start, length);

        public override bool Equals(object? obj)
        {
            if (obj is FixedUtf8String p)
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
