using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="TYPEDEFSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct TypeDefSymType
    {
        private const int typindOffset16 = 2;
        private const int nameOffset16 = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="TYPEDEFSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="TYPEDEFSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="TYPEDEFSYMTYPE.typind"/>
        public short typind => GetPostOffsetInt16(value, typindOffset16);

        /// <inheritdoc cref="TYPEDEFSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal TypeDefSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
