using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, UTF-16 character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct Utf16String :
        IString<Utf16String, char>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly char* Value;

        private string DebuggerDisplay => this.ToString();

        public Utf16String(char* value) => this.Value = value;

        public int Length => StringHelpers.GetWideStringLength(Value);

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
        #region IEquatable / IComparable (Utf16String)

        public bool Equals(Utf16String other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        public int CompareTo(Utf16String other) =>
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

        public static implicit operator char*(Utf16String value) => value.Value;

        public static explicit operator Utf16String(char* value) => new Utf16String(value);

        //Equality

        public static bool operator ==(Utf16String left, string right) => left.Equals(right);
        public static bool operator !=(Utf16String left, string right) => !left.Equals(right);

        public static bool operator ==(string left, Utf16String right) => right.Equals(left);
        public static bool operator !=(string left, Utf16String right) => !right.Equals(left);

        public static bool operator ==(Utf16String left, Utf16String right) => right.Equals(left);
        public static bool operator !=(Utf16String left, Utf16String right) => !right.Equals(left);

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is Utf16String p)
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
        public override string ToString() => Value is null ? null! : new string(Value);
    }
}
