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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ16_t typind => value->typind;

        public byte count => value->count;

        internal ManyRegSym16t(MANYREGSYM_16t* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }
    }
}

