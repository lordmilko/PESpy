using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMIA64"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymIA64 : IViewable
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
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //retReg
            1;               //flags

        internal ProcSymIA64(PROCSYMIA64* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYMIA64, this, ViewKind.ProcSymIA64, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(retReg), retReg);
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
