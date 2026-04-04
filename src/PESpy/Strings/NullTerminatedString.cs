using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Roslyn.Utilities;

namespace PESpy
{
    public enum StringKind
    {
        ANSI = 1,
        UTF8 = 2,
        UTF16 = 3
    }

    /// <summary>
    /// A pointer to a null-terminated, constant string of any kind (ANSI, UTF-8 or UTF-16)
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct NullTerminatedString :
        IString<NullTerminatedString, byte>,
        IEquatable<string>,
        IComparable<string>
    {
        public readonly byte* Value;

        public StringKind Kind { get; }

        private string DebuggerDisplay => this.ToString();

        public NullTerminatedString(byte* value, StringKind kind)
        {
            Value = value;
            Kind = kind;
        }

        public int Length
        {
            get
            {
                if (Kind == StringKind.UTF16)
                    return StringHelpers.GetWideStringLength((char*) Value);
                else
                    return StringHelpers.GetStringLength(Value);
            }
        }

        #region IString

        public bool StartsWith(string value)
        {
            if (Kind == StringKind.UTF16)
                return AsWideSpan().StartsWith(value.AsSpan());

            return StringHelpers.StartsWith(AsSpan(), value);
        }

        public bool EndsWith(string value)
        {
            if (Kind == StringKind.UTF16)
                return AsWideSpan().EndsWith(value.AsSpan());

            return StringHelpers.EndsWith(AsSpan(), value);
        }

        public bool Contains(string value)
        {
            if (Kind == StringKind.UTF16)
                 return AsWideSpan().IndexOf(value.AsSpan()) != -1;

            return StringHelpers.Contains(AsSpan(), value);
        }

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public void CopyTo(Span<char> destination)
        {
            if (Kind == StringKind.UTF16)
                AsWideSpan().CopyTo(destination);
            else
                StringHelpers.CopyTo(AsSpan(), destination);
        }

        public Span<byte> AsSpan()
        {
            var length = Length;

            if (Kind == StringKind.UTF16)
                length *= 2;

            return new Span<byte>(Value, length);
        }

        public Span<char> AsWideSpan() =>
            new Span<char>(Value, StringHelpers.GetWideStringLength((char*) Value));

        #endregion
        #region IEquatable / IComparable (NullTerminatedString)

        public bool Equals(NullTerminatedString other) =>
            AsSpan().SequenceEqual(other.AsSpan());

        public int CompareTo(NullTerminatedString other) =>
            AsSpan().SequenceCompareTo(other.AsSpan());

        #endregion
        #region IEquatable / IComparable (string)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string? other)
        {
            if (other == null)
                return Value == default;

            return Equals(other.AsSpan());
        }

        public bool Equals(ReadOnlySpan<char> other)
        {
            if (Kind == StringKind.UTF16)
                return AsWideSpan().SequenceEqual(other);

            return StringHelpers.Equals(Value, Length, other);
        }

        public int CompareTo(string other)
        {
            if (Kind == StringKind.UTF16)
                return AsWideSpan().SequenceCompareTo(other.AsSpan());

            return StringHelpers.CompareTo(AsSpan(), other);
        }

        public int CompareToIgnoreCase(NullTerminatedString other)
        {
            if (Kind == StringKind.UTF16)
            {
                if (other.Kind == StringKind.UTF16)
                    return StringHelpers.CompareToIgnoreCase(AsWideSpan(), other.AsWideSpan());

                return StringHelpers.CompareToIgnoreCase(AsWideSpan(), other.AsSpan());
            }

            if (other.Kind == StringKind.UTF16)
                return StringHelpers.CompareToIgnoreCase(AsSpan(), other.AsWideSpan());

            return StringHelpers.CompareToIgnoreCase(AsSpan(), other.AsSpan());
        }

        #endregion
        #region Operators

        //Conversions

        //No implicit conversion from byte* to NullTerminatedString; you must specify a StringKind

        public static implicit operator byte*(NullTerminatedString value) => value.Value;

        //Equality

        public static bool operator ==(NullTerminatedString left, string right) => left.Equals(right);
        public static bool operator !=(NullTerminatedString left, string right) => !left.Equals(right);

        public static bool operator ==(string left, NullTerminatedString right) => right.Equals(left);
        public static bool operator !=(string left, NullTerminatedString right) => !right.Equals(left);

        public static bool operator ==(NullTerminatedString left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(NullTerminatedString left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator ==(ReadOnlySpan<char> left, NullTerminatedString right) => right.Equals(left);
        public static bool operator !=(ReadOnlySpan<char> left, NullTerminatedString right) => !right.Equals(left);

        public static bool operator ==(NullTerminatedString left, NullTerminatedString right) => right.Equals(left);
        public static bool operator !=(NullTerminatedString left, NullTerminatedString right) => !right.Equals(left);

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is NullTerminatedString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            if (obj == null)
                return Value == default;

            return false;
        }

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        public override string ToString()
        {
            if (Value is null)
                return null;

            switch (Kind)
            {
                case StringKind.UTF16:
                    return new string((char*) Value);

                case StringKind.UTF8:
                    return new string((sbyte*) Value, 0, Length, Encoding.UTF8);

                default:
                    return new string((sbyte*) Value, 0, Length, Encoding.Default);
            }
        }
    }
}
