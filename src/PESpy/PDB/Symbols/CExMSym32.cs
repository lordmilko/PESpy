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

        /// <inheritdoc cref="CEXMSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CEXMSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CEXMSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="CEXMSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="CEXMSYM32.model"/>
        public short model => value->model;

        /// <inheritdoc cref="CEXMSYM32.pcdtable"/>
        public CV_uoff32_t pcdtable => value->pcdtable;

        /// <inheritdoc cref="CEXMSYM32.pcdspi"/>
        public CV_uoff32_t pcdspi => value->pcdspi;

        /// <inheritdoc cref="CEXMSYM32.subtype"/>
        public CV_COBOL_e subtype => value->subtype;

        /// <inheritdoc cref="CEXMSYM32.flag"/>
        public short flag => value->flag;

        /// <inheritdoc cref="CEXMSYM32.calltableOff"/>
        public CV_uoff32_t calltableOff => value->calltableOff;

        /// <inheritdoc cref="CEXMSYM32.calltableSeg"/>
        public short calltableSeg => value->calltableSeg;

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //model
            sizeof(uint)   + //pcdtable
            sizeof(uint)   + //pcdspi
            sizeof(ushort) + //subtype
            sizeof(short)  + //flag
            sizeof(uint)   + //calltableOff
            sizeof(short);   //calltableSeg

        internal CExMSym32(CEXMSYM32* value)
        {
            this.value = value;
        }
    }
}

