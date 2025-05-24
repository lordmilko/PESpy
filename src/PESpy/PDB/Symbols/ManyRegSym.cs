using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM* value;

        /// <inheritdoc cref="MANYREGSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM.count"/>
        public byte count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(byte);    //count

        internal ManyRegSym(MANYREGSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }
    }
}

