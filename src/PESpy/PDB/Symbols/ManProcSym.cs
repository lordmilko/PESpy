using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ManProcSym : IViewable
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
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MANPROCSYM, this, ViewKind.ManProcSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(pParent), pParent);
            s.WriteField(nameof(pEnd), pEnd);
            s.WriteField(nameof(pNext), pNext);
            s.WriteField(nameof(len), len);
            s.WriteField(nameof(DbgStart), DbgStart);
            s.WriteField(nameof(DbgEnd), DbgEnd);
            s.WriteField(nameof(token), token);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(flags), flags);
            s.WriteField(nameof(retReg), retReg);
            s.WriteSymStringField(nameof(name), SymType.ReadString(value, value->name, viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
