using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="REGSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct RegSymType
    {
        private const int typindOffset16 = 2;
        private const int regOffset16 = 4;
        private const int nameOffset16 = 5;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="REGSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="REGSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="REGSYMTYPE.typind"/>
        public short typind => GetPostOffsetInt16(value, typindOffset16);

        /// <inheritdoc cref="REGSYMTYPE.reg"/>
        public byte reg => GetPostOffsetByte(value, regOffset16);

        /// <inheritdoc cref="REGSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal RegSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
