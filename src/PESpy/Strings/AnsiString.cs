using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, ANSI character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct AnsiString :
        IString<AnsiString, byte>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly byte* Value;

        private string DebuggerDisplay => this.ToString();

        public AnsiString(byte* value) => this.Value = value;

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
        #region IEquatable / IComparable (AnsiString)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AnsiString other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(AnsiString other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        #endregion
        #region IEquatable / IComparable (string)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            return StringHelpers.Equals(Value, Length, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsIgnoreCase(string? other)
        {
            if (other == null)
                return Value == default;

            return StringHelpers.EqualsIgnoreCase(Value, Length, other);
        }

        public int CompareTo(string other)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Operators

        //Conversions

        public static implicit operator byte*(AnsiString value) => value.Value;

        public static explicit operator AnsiString(byte* value) => new AnsiString(value);

        public static explicit operator FixedAnsiString(AnsiString value) => new FixedAnsiString(value.Value, value.Length);

        public static explicit operator FixedUtf8String(AnsiString value) => new FixedUtf8String(value.Value, value.Length);

        //Equality

        public static bool operator ==(AnsiString left, string right) => left.Equals(right);
        public static bool operator !=(AnsiString left, string right) => !left.Equals(right);

        public static bool operator ==(string left, AnsiString right) => right.Equals(left);
        public static bool operator !=(string left, AnsiString right) => !right.Equals(left);

        public static bool operator ==(AnsiString left, AnsiString right) => right.Equals(left);
        public static bool operator !=(AnsiString left, AnsiString right) => !right.Equals(left);

        #endregion

        public override bool Equals(object? obj)
        {
            if (obj is AnsiString p)
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
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, global::System.Text.Encoding.Default);
    }
}
