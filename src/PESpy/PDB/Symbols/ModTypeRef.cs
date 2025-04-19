using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MODTYPEREF"/> structure.
    /// </summary>
    public readonly unsafe struct ModTypeRef
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MODTYPEREF* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public bool fNone => value->fNone;

        public bool fRefTMPCT => value->fRefTMPCT;

        public bool fOwnTMPCT => value->fOwnTMPCT;

        public bool fOwnTMR => value->fOwnTMR;

        public bool fOwnTM => value->fOwnTM;

        public bool fRefTM => value->fRefTM;

        public int reserved => value->reserved;

        public short word0 => value->word0;
        public short word1 => value->word1;

        internal ModTypeRef(MODTYPEREF* value)
        {
            this.value = value;
        }
    }
}

