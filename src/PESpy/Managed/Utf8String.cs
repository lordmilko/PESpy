using System;
using System.Diagnostics;

namespace PESpy
{
    /// <summary>
    /// A pointer to a null-terminated, constant, UTF-8 character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly unsafe struct Utf8String : IEquatable<Utf8String>, IEquatable<string>
    {
        public readonly byte* Value;

        public Utf8String(byte* value) => this.Value = value;

        public static implicit operator byte*(Utf8String value) => value.Value;

        public static explicit operator Utf8String(byte* value) => new Utf8String(value);

        public bool Equals(Utf8String other) => this.Value == other.Value;

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

        public static bool operator ==(Utf8String left, string right) => left.Equals(right);
        public static bool operator !=(Utf8String left, string right) => left.Equals(right);

        public static bool operator ==(string left, Utf8String right) => right.Equals(left);
        public static bool operator !=(string left, Utf8String right) => right.Equals(left);

        public override bool Equals(object obj)
        {
            if (obj is Utf8String p)
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
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, System.Text.Encoding.UTF8);

        private string DebuggerDisplay => this.ToString();
    }
}
