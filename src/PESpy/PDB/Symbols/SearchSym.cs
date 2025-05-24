using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SEARCHSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SearchSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEARCHSYM* value;

        /// <inheritdoc cref="SEARCHSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEARCHSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEARCHSYM.startsym"/>
        public int startsym => value->startsym;

        /// <inheritdoc cref="SEARCHSYM.seg"/>
        public ushort seg => value->seg;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //startsym
            sizeof(short);   //seg

        internal SearchSym(SEARCHSYM* value)
        {
            this.value = value;
        }
    }
}

