using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMMIPS_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymMips16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMMIPS_16t* value;

        /// <inheritdoc cref="PROCSYMMIPS_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYMMIPS_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYMMIPS_16t.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.regSave"/>
        public int regSave => value->regSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpSave"/>
        public int fpSave => value->fpSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.intOff"/>
        public CV_uoff32_t intOff => value->intOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpOff"/>
        public CV_uoff32_t fpOff => value->fpOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYMMIPS_16t.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.typind"/>
        public CV_typ16_t typind => value->typind;

        /// <inheritdoc cref="PROCSYMMIPS_16t.retReg"/>
        public byte retReg => value->retReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.frameReg"/>
        public byte frameReg => value->frameReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(int)    + //len
            sizeof(int)    + //DbgStart
            sizeof(int)    + //DbgEnd
            sizeof(int)    + //regSave
            sizeof(int)    + //fpSave
            sizeof(uint)   + //intOff
            sizeof(uint)   + //fpOff
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            sizeof(byte)   + //retReg
            sizeof(byte);    //frameReg

        internal ProcSymMips16t(PROCSYMMIPS_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

