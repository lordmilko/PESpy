using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMMIPS"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymMips
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMMIPS* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public int len => value->len;

        public int DbgStart => value->DbgStart;

        public int DbgEnd => value->DbgEnd;

        public int regSave => value->regSave;

        public int fpSave => value->fpSave;

        public CV_uoff32_t intOff => value->intOff;

        public CV_uoff32_t fpOff => value->fpOff;

        public CV_typ_t typind => value->typind;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public byte retReg => value->retReg;

        public byte frameReg => value->frameReg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal ProcSymMips(PROCSYMMIPS* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

