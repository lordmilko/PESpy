using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="LABSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct LabSymType
    {
        private const int offOffset16 = 2;
        private const int rtntypOffset16 = 4;
        private const int nameOffset16 = 5;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="LABSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="LABSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="LABSYMTYPE.off"/>
        public int off => GetOffset(value, offOffset16);

        /// <inheritdoc cref="LABSYMTYPE.rtntyp"/>
        public byte rtntyp => GetPostOffsetByte(value, rtntypOffset16);

        /// <inheritdoc cref="LABSYMTYPE.name"/>
        public SymString name => GetPostOffsetString(value, nameOffset16, reclen);

        internal LabSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
