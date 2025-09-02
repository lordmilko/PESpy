using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ManProcSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANPROCSYM* value;

        /// <inheritdoc cref="MANPROCSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANPROCSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANPROCSYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="MANPROCSYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="MANPROCSYM.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="MANPROCSYM.len"/>
        public int len => value->len;

        /// <inheritdoc cref="MANPROCSYM.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="MANPROCSYM.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="MANPROCSYM.token"/>
        public mdToken token => value->token;

        /// <inheritdoc cref="MANPROCSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="MANPROCSYM.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="MANPROCSYM.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="MANPROCSYM.retReg"/>
        public short retReg => value->retReg;

        /// <inheritdoc cref="MANPROCSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

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
            sizeof(int)    + //token
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            1              + //flags
            sizeof(short);   //retReg

        internal ManProcSym(MANPROCSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

