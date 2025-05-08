using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYM16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public short len => value->len;

        public short DbgStart => value->DbgStart;

        public short DbgEnd => value->DbgEnd;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public CV_typ16_t typind => value->typind;

        public CV_PROCFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal ProcSym16(PROCSYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

