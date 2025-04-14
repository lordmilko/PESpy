#if PEFAST
using System;
using System.Diagnostics;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, UTF-16 character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct Utf16String : IEquatable<Utf16String>, IEquatable<string>
    {
        public readonly char* Value;

        public Utf16String(char* value) => this.Value = value;

        public static implicit operator char*(Utf16String value) => value.Value;

        public static explicit operator Utf16String(char* value) => new Utf16String(value);

        public bool Equals(Utf16String other) => this.Value == other.Value;

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

        public static bool operator ==(Utf16String left, string right) => left.Equals(right);
        public static bool operator !=(Utf16String left, string right) => left.Equals(right);

        public static bool operator ==(string left, Utf16String right) => right.Equals(left);
        public static bool operator !=(string left, Utf16String right) => right.Equals(left);

        public override bool Equals(object obj)
        {
            if (obj is AnsiString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public override int GetHashCode() => unchecked((int) this.Value);

        public int Length
        {
            get
            {
                char* p = this.Value;

                if (p is null)
                    return 0;

                while (*p != '\0')
                    p++;

                return checked((int) (p - this.Value));
            }
        }

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string(this.Value);

        private string DebuggerDisplay => this.ToString();
    }
}
#endif
