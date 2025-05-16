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

        /// <inheritdoc cref="MODTYPEREF.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MODTYPEREF.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MODTYPEREF.fNone"/>
        public bool fNone => value->fNone;

        /// <inheritdoc cref="MODTYPEREF.fRefTMPCT"/>
        public bool fRefTMPCT => value->fRefTMPCT;

        /// <inheritdoc cref="MODTYPEREF.fOwnTMPCT"/>
        public bool fOwnTMPCT => value->fOwnTMPCT;

        /// <inheritdoc cref="MODTYPEREF.fOwnTMR"/>
        public bool fOwnTMR => value->fOwnTMR;

        /// <inheritdoc cref="MODTYPEREF.fOwnTM"/>
        public bool fOwnTM => value->fOwnTM;

        /// <inheritdoc cref="MODTYPEREF.fRefTM"/>
        public bool fRefTM => value->fRefTM;

        /// <inheritdoc cref="MODTYPEREF.reserved"/>
        public int reserved => value->reserved;

        /// <inheritdoc cref="MODTYPEREF.word0"/>
        public short word0 => value->word0;

        /// <inheritdoc cref="MODTYPEREF.word1"/>
        public short word1 => value->word1;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //data
            sizeof(short)  + //word0
            sizeof(short);   //word1

        internal ModTypeRef(MODTYPEREF* value)
        {
            this.value = value;
        }
    }
}

