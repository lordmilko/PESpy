using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, ANSI character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct AnsiString : IEquatable<AnsiString>, IEquatable<string>
    {
        public readonly byte* Value;

        public AnsiString(byte* value) => this.Value = value;

        public void CopyTo(Span<byte> destination) => new Span<byte>(Value, Length).CopyTo(destination);

        public static implicit operator byte*(AnsiString value) => value.Value;

        public static explicit operator AnsiString(byte* value) => new AnsiString(value);

        public static explicit operator FixedAnsiString(AnsiString value) => new FixedAnsiString(value.Value, value.Length);

        public static explicit operator FixedUtf8String(AnsiString value) => new FixedUtf8String(value.Value, value.Length);

        public bool Equals(AnsiString other) => this.Value == other.Value;

        public bool Equals(string? other)
        {
            if (other == null)
                return false;

            var length = Length;

            if (other.Length != length)
                return false;

            fixed (char* p = other)
            {
                for (var i = 0; i < length; i++)
                {
                    if ((byte) p[i] != Value[i])
                        return false;
                }
            }

            return true;
        }

        public static bool operator ==(AnsiString left, string right) => left.Equals(right);
        public static bool operator !=(AnsiString left, string right) => !left.Equals(right);

        public static bool operator ==(string left, AnsiString right) => right.Equals(left);
        public static bool operator !=(string left, AnsiString right) => !right.Equals(left);

        public bool EndsWith(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var length = Length;

            if (value.Length > length)
                return false;

            var span = new Span<byte>(Value + (length - value.Length), value.Length);

            fixed (char* p = value)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    if ((byte) p[i] != span[i])
                        return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is AnsiString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public Span<byte> AsSpan() => new Span<byte>(Value, Length);

        public override int GetHashCode() => Hash.GetFNVHashCode(AsSpan());

        public int Length
        {
            get
            {
                byte* p = this.Value;

                if (p is null)
                    return 0;

                while (*p != 0)
                    p++;

                return checked((int) (p - this.Value));
            }
        }

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, global::System.Text.Encoding.Default);

        private string DebuggerDisplay => this.ToString();
    }
}
