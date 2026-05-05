using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="LOCSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct LocSymType
    {
        private const int offOffset16 = 2;
        private const int segOffset16 = 4;
        private const int typindOffset16 = 6;
        private const int nameOffset16 = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="LOCSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="LOCSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="LOCSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="LOCSYMTYPE.seg"/>
        public ushort seg => (ushort) GetPostOffsetInt16(value, segOffset16);

        /// <inheritdoc cref="LOCSYMTYPE.typind"/>
        public short typind => GetPostOffsetInt16(value, typindOffset16);

        /// <inheritdoc cref="LOCSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal LocSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
