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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int startsym => value->startsym;

        public short seg => value->seg;

        internal SearchSym(SEARCHSYM* value)
        {
            this.value = value;
        }
    }
}

