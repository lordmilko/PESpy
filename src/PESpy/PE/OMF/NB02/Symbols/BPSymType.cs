using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="BPSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct BPSymType
    {
        private const int offOffset16 = 2;
        private const int typindOffset16 = 4;
        private const int nameOffset16 = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="BPSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="BPSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="BPSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="BPSYMTYPE.typind"/>
        public short typind => GetPostOffsetInt16(value, offOffset16);

        /// <inheritdoc cref="BPSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal BPSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
