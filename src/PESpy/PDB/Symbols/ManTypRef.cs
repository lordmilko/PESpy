using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANTYPREF"/> structure.
    /// </summary>
    public readonly unsafe struct ManTypRef
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANTYPREF* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        internal ManTypRef(MANTYPREF* value)
        {
            this.value = value;
        }
    }
}

