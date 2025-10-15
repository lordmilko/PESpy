using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMMIPS_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymMips16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMMIPS_16t* value;

        /// <inheritdoc cref="PROCSYMMIPS_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYMMIPS_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYMMIPS_16t.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.regSave"/>
        public int regSave => value->regSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpSave"/>
        public int fpSave => value->fpSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.intOff"/>
        public CV_uoff32_t intOff => value->intOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpOff"/>
        public CV_uoff32_t fpOff => value->fpOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYMMIPS_16t.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYMMIPS_16t.retReg"/>
        public byte retReg => value->retReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.frameReg"/>
        public byte frameReg => value->frameReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.name"/>
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
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            sizeof(byte)   + //retReg
            sizeof(byte);    //frameReg

        internal ProcSymMips16t(PROCSYMMIPS_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYMMIPS_16t, this, ViewKind.ProcSymMips16t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(typind), typind);
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
