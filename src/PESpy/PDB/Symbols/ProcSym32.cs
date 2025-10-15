using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym32 : IViewable
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
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYM32.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int) +    //pParent
            sizeof(int) +    //pEnd
            sizeof(int) +    //pNext
            sizeof(int) +    //len
            sizeof(int) +    //DbgStart
            sizeof(int) +    //DbgEnd
            sizeof(int) +    //typind
            sizeof(int) +    //off
            sizeof(short) +  //seg
            sizeof(byte);    //flags

        internal ProcSym32(PROCSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYM32, this, ViewKind.ProcSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
