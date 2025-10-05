using System;
using Roslyn.Utilities;

namespace PESpy
{
    public readonly unsafe struct SymString
    {
        public readonly byte* Value;
        public readonly bool IsLengthPrefixed;

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
                    return *(Value - 1);

                return StringHelpers.GetStringLength(Value);
            }
        }

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(char[] array)
        {
            var value = Value;

            for (var i = 0; i < Length; i++)
                array[i] = (char) value[i];
        }

        public bool Equals(SymString other)
        {
            if (Value == other.Value)
                return true;

            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            return StringHelpers.Equals(Value, other);
        }

        public static implicit operator SymString(Utf8String value) => new SymString(value.Value, isLengthPrefixed: false);

        public static implicit operator SymString(AnsiString value) => new SymString(value.Value, isLengthPrefixed: false);

        public static bool operator ==(SymString left, string? right) => left.Equals(right);
        public static bool operator !=(SymString left, string? right) => !left.Equals(right);

        public static bool operator ==(string? left, SymString right) => right.Equals(left);
        public static bool operator !=(string? left, SymString right) => !right.Equals(left);

        public static bool operator ==(SymString left, SymString right) => Equals(left, right);
        public static bool operator !=(SymString left, SymString right) => !Equals(left, right);

        public override bool Equals(object? obj)
        {
            if (obj is FixedUtf8String p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public bool StartsWith(string value)
        {
            if (value.Length > Length)
                return false;

            //We currently only support ANSI values

            for (var i = 0; i < value.Length; i++)
            {
                if ((byte) value[i] != Value[i])
                    return false;
            }

            return true;
        }

        public Span<byte> AsSpan() =>
            new Span<byte>(Value, IsLengthPrefixed ? *(Value - 1) : StringHelpers.GetStringLength(Value));

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);

        //todo: implement icomparable for all of our other string types
        public int CompareTo(SymString other) => AsSpan().SequenceCompareTo(other.AsSpan());

        public int CompareTo(string other) => throw new NotImplementedException();

        private string DebuggerDisplay => this.ToString();
    }
}
