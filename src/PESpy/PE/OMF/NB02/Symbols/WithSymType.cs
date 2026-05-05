using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="WITHSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct WithSymType
    {
        private const int offOffset16 = 2;
        private const int lenOffset16 = 4;
        private const int nameOffset16 = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="WITHSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="WITHSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="WITHSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="WITHSYMTYPE.len"/>
        public short len => GetPostOffsetInt16(value, lenOffset16);

        /// <inheritdoc cref="WITHSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal WithSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
