using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public short model => value->model;

        public CV_uoff32_t pcdtable => value->pcdtable;
        public CV_uoff32_t pcdspi => value->pcdspi;

        public CV_COBOL_e subtype => value->subtype;
        public short flag => value->flag;

        public CV_uoff32_t calltableOff => value->calltableOff;
        public short calltableSeg => value->calltableSeg;

        internal CExMSym32(CEXMSYM32* value)
        {
            this.value = value;
        }
    }
}

