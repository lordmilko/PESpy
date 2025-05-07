using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public short model => value->model;

        public CV_uoff16_t pcdtable => value->pcdtable;
        public CV_uoff16_t pcdspi => value->pcdspi;

        public CV_COBOL_e subtype => value->subtype;
        public short flag => value->flag;

        #region PESpy

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal CExMSym16(CEXMSYM16* value)
        {
            this.value = value;
        }
    }
}

