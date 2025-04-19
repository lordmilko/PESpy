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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public byte count => value->count;

        internal ManyRegSym(MANYREGSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }
    }
}

