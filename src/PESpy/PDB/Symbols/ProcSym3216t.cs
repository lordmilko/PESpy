using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym3216t : IViewable
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
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYM32_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYM32_16t.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM32_16t.name"/>
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
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            1;               //flags

        internal ProcSym3216t(PROCSYM32_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYM32_16t, this, ViewKind.ProcSym3216t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(flags), flags);
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
