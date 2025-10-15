using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym2 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM2* value;

        /// <inheritdoc cref="REFSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM2.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM2.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM2.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFSYM2.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short);   //imod

        internal RefSym2(REFSYM2* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFSYM2, this, ViewKind.RefSym2, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(sumName), sumName);
            s.WriteField(nameof(ibSym), ibSym);
            s.WriteField(nameof(imod), imod);
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
