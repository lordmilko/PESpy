using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace PESpy
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct FixedUtf16String :
        IString<FixedUtf16String, char>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly char* Value;
        public int Length { get; }

        private string DebuggerDisplay => this.ToString();

        public FixedUtf16String(char* value, int length)
        {
            Value = value;
            Length = length;
        }

        #region IString

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) => AsSpan().StartsWith(value.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(string value) => AsSpan().EndsWith(value.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string value) => AsSpan().IndexOf(value.AsSpan()) != -1;

        public void CopyTo(Span<char> destination) => new Span<char>(Value, Length).CopyTo(destination);

        public Span<char> AsSpan() => new Span<char>(Value, Length);

        #endregion
        #region IEquatable / IComparable (FixedUtf16String)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FixedUtf16String other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FixedUtf16String other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        #endregion
        #region IEquatable / IComparable (string)

        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public int CompareTo(string other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        #endregion
        #region Operators

        //Conversions

        public static implicit operator Span<char>(FixedUtf16String value) => new Span<char>(value.Value, value.Length);

        //Equality

        public static bool operator ==(FixedUtf16String left, string right) => left.Equals(right);
        public static bool operator !=(FixedUtf16String left, string right) => !left.Equals(right);

        public static bool operator ==(string left, FixedUtf16String right) => right.Equals(left);
        public static bool operator !=(string left, FixedUtf16String right) => !right.Equals(left);

        public static bool operator ==(FixedUtf16String left, FixedUtf16String right) => right.Equals(left);
        public static bool operator !=(FixedUtf16String left, FixedUtf16String right) => !right.Equals(left);

        #endregion

        public override bool Equals(object? obj)
        {
            if (obj is FixedUtf16String p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => Value is null ? null! : new string(Value, 0, Length);
    }
}
