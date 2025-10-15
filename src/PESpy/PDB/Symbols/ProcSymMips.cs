using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMMIPS"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymMips : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMMIPS* value;

        /// <inheritdoc cref="PROCSYMMIPS.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYMMIPS.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYMMIPS.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYMMIPS.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYMMIPS.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYMMIPS.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYMMIPS.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYMMIPS.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYMMIPS.regSave"/>
        public int regSave => value->regSave;

        /// <inheritdoc cref="PROCSYMMIPS.fpSave"/>
        public int fpSave => value->fpSave;

        /// <inheritdoc cref="PROCSYMMIPS.intOff"/>
        public CV_uoff32_t intOff => value->intOff;

        /// <inheritdoc cref="PROCSYMMIPS.fpOff"/>
        public CV_uoff32_t fpOff => value->fpOff;

        /// <inheritdoc cref="PROCSYMMIPS.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYMMIPS.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYMMIPS.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYMMIPS.retReg"/>
        public byte retReg => value->retReg;

        /// <inheritdoc cref="PROCSYMMIPS.frameReg"/>
        public byte frameReg => value->frameReg;

        /// <inheritdoc cref="PROCSYMMIPS.name"/>
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
            sizeof(int)    + //regSave
            sizeof(int)    + //fpSave
            sizeof(uint)   + //intOff
            sizeof(uint)   + //fpOff
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(byte)   + //retReg
            sizeof(byte);    //frameReg

        internal ProcSymMips(PROCSYMMIPS* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYMMIPS, this, ViewKind.ProcSymMips, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
            s.WriteField(nameof(regSave), regSave);
            s.WriteField(nameof(fpSave), fpSave);
            s.WriteField(nameof(intOff), intOff);
            s.WriteField(nameof(fpOff), fpOff);
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(retReg), retReg);
            s.WriteField(nameof(frameReg), frameReg);
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
