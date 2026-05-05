using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="PROCSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymType
    {
        private const int offOffset16 = 2;
        private const int typindOffset16 = 4;
        private const int lenOffset16 = 6;
        private const int startoffOffset16 = 8;
        private const int endoffOffset16 = 10;
        private const int resOffset16 = 12;
        private const int rtntypOffset16 = 14;
        private const int nameOffset16 = 15;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="PROCSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="PROCSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="PROCSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.typind"/>
        public short typind => GetPostOffsetInt16(value, typindOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.len"/>
        public short len => GetPostOffsetInt16(value, lenOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.startoff"/>
        public short startoff => GetPostOffsetInt16(value, startoffOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.endoff"/>
        public short endoff => GetPostOffsetInt16(value, endoffOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.res"/>
        public short res => GetPostOffsetInt16(value, resOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.rectyp"/>
        public byte rtntyp => GetPostOffsetByte(value, rtntypOffset16);

        /// <inheritdoc cref="PROCSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal ProcSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
