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

        /// <inheritdoc cref="PROCSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYM16.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="PROCSYM16.DbgStart"/>
        public short DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYM16.DbgEnd"/>
        public short DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="PROCSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYM16.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYM16.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(short)  + //len
            sizeof(short)  + //DbgStart
            sizeof(short)  + //DbgEnd
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            1;               //flags

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

