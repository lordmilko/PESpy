using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="BLKSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct BlkSymType
    {
        private const int offOffset16 = 2;
        private const int lenOffset16 = 4;
        private const int nameOffset16 = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="BLKSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="BLKSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="BLKSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="BLKSYMTYPE.len"/>
        public short len => GetPostOffsetInt16(value, lenOffset16);

        /// <inheritdoc cref="BLKSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        public bool Is32Bit => GetIs32Bit(value);

        internal BlkSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
