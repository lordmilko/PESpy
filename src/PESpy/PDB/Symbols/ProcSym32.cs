using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYM32* value;

        /// <inheritdoc cref="PROCSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYM32.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYM32.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYM32.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYM32.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYM32.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYM32.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYM32.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="PROCSYM32.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM32.name"/>
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
            sizeof(int);     //DbgEnd

        internal ProcSym32(PROCSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

