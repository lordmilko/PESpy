using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OEMSYMBOL"/> structure.
    /// </summary>
    public readonly unsafe struct OemSymbol
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OEMSYMBOL* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public Guid idOem => *(Guid*) value->idOem;

        public CV_typ_t typind => value->typind;

        internal OemSymbol(OEMSYMBOL* value)
        {
            this.value = value;
            Debug.Assert(false, "Read rgl");
        }
    }
}

