using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYM32_16t* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public int len => value->len;

        public int DbgStart => value->DbgStart;

        public int DbgEnd => value->DbgEnd;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public CV_typ16_t typind => value->typind;

        public CV_PROCFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal ProcSym3216t(PROCSYM32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

