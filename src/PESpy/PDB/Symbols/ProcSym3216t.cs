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

        /// <inheritdoc cref="PROCSYM32_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYM32_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYM32_16t.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYM32_16t.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYM32_16t.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYM32_16t.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYM32_16t.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYM32_16t.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYM32_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYM32_16t.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="PROCSYM32_16t.typind"/>
        public CV_typ16_t typind => value->typind;

        /// <inheritdoc cref="PROCSYM32_16t.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM32_16t.name"/>
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
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            1;               //flags

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

