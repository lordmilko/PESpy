using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM2* value;

        /// <inheritdoc cref="MANYREGSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM2.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM2.count"/>
        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short);   //count

        internal ManyRegSym2(MANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg"); //CV_HREG_e?
        }
    }
}

