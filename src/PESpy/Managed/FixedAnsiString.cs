namespace PESpy
{
    public readonly unsafe struct FixedAnsiString
    {
        public readonly byte* Value;
        public readonly int Length;

        public FixedAnsiString(byte* value, int length)
        {
            Value = value;
            Length = length;
        }

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

        public static bool operator ==(FixedAnsiString left, string right) => left.Equals(right);
        public static bool operator !=(FixedAnsiString left, string right) => left.Equals(right);

        public static bool operator ==(string left, FixedAnsiString right) => right.Equals(left);
        public static bool operator !=(string left, FixedAnsiString right) => right.Equals(left);

        public override bool Equals(object obj)
        {
            if (obj is FixedAnsiString p)
                return Equals(p);

            if (obj is string s)
                return Equals(s);

            return false;
        }

        public override int GetHashCode() => unchecked((int) this.Value);

        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, decoding as UTF-8.
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null! : new string((sbyte*) this.Value, 0, this.Length, global::System.Text.Encoding.Default);

        private string DebuggerDisplay => this.ToString();
    }
}
