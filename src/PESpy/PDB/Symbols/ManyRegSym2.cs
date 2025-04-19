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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public short count => value->count;

        internal ManyRegSym2(MANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }
    }
}

