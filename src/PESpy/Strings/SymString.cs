using System;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace PESpy
{
    public readonly unsafe struct SymString :
        IString<SymString, byte>,
        IEquatable<string>,
        IComparable<string>,
        IComparable<SymString>
    {
        public readonly byte* Value;
        public readonly bool IsLengthPrefixed;

        private string DebuggerDisplay => this.ToString();

        internal SymString(byte* value, bool isLengthPrefixed)
        {
            Value = value;
            IsLengthPrefixed = isLengthPrefixed;
        }

        public int Length
        {
            get
            {
                if (IsLengthPrefixed)
                {
                    if (Value == default)
                        return 0;

                    return *(Value - 1);
                }

                return StringHelpers.GetStringLength(Value);
            }
        }

        #region IString

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) =>
            StringHelpers.StartsWith(AsSpan(), value); //We can't pass a byte* because we're not necessarily null terminated

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(string value) =>
            StringHelpers.EndsWith(AsSpan(), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string value) =>
            StringHelpers.Contains(AsSpan(), value);

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(Span<char> destination) => StringHelpers.CopyTo(AsSpan(), destination);

        public Span<byte> AsSpan() =>
            new Span<byte>(Value, IsLengthPrefixed ? *(Value - 1) : StringHelpers.GetStringLength(Value));

        #endregion
        #region IEquatable / IComparable (FixedUtf16String)

        public bool Equals(SymString other)
        {
            if (Value == other.Value)
                return true;

            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public int CompareTo(SymString other) => AsSpan().SequenceCompareTo(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareToIgnoreCase(SymString other) =>
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

        public int CompareTo(string other) => throw new NotImplementedException();

        #endregion
        #region Operators

        //Conversions

        public static implicit operator SymString(Utf8String value) => new SymString(value.Value, isLengthPrefixed: false);

        public static implicit operator SymString(AnsiString value) => new SymString(value.Value, isLengthPrefixed: false);
        public static implicit operator SymString(FixedAnsiString value) => new SymString(value.Value, isLengthPrefixed: false);

        public static implicit operator FixedUtf8String(SymString value) => new FixedUtf8String(value.Value, value.Length);

        //Equality

        public static bool operator ==(SymString left, string? right) => left.Equals(right);
        public static bool operator !=(SymString left, string? right) => !left.Equals(right);

        public static bool operator ==(string? left, SymString right) => right.Equals(left);
        public static bool operator !=(string? left, SymString right) => !right.Equals(left);

        public static bool operator ==(SymString left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(SymString left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator ==(ReadOnlySpan<char> left, SymString right) => right.Equals(left);
        public static bool operator !=(ReadOnlySpan<char> left, SymString right) => !right.Equals(left);

        public static bool operator ==(SymString left, SymString right) => Equals(left, right);
        public static bool operator !=(SymString left, SymString right) => !Equals(left, right);

        #endregion

        public override bool Equals(object? obj)
        {
            if (obj is SymString p)
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
        public override string ToString() => this.Value is null ? "<null>" : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);
    }
}
