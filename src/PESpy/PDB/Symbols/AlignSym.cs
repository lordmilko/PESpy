using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ALIGNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AlignSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ALIGNSYM* value;

        /// <inheritdoc cref="ALIGNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ALIGNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        //No fields

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal AlignSym(ALIGNSYM* value)
        {
            this.value = value;
        }
    }
}

