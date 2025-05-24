using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM_16t* value;

        /// <inheritdoc cref="MANYREGSYM_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM_16t.count"/>
        public byte count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(byte);    //count

        internal ManyRegSym16t(MANYREGSYM_16t* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }
    }
}

