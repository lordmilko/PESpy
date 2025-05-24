using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMIA64"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymIA64
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMIA64* value;

        /// <inheritdoc cref="PROCSYMIA64.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYMIA64.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYMIA64.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYMIA64.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYMIA64.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYMIA64.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYMIA64.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYMIA64.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYMIA64.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYMIA64.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYMIA64.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYMIA64.retReg"/>
        public short retReg => value->retReg;

        /// <inheritdoc cref="PROCSYMIA64.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYMIA64.name"/>
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
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //retReg
            1;               //flags

        internal ProcSymIA64(PROCSYMIA64* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

