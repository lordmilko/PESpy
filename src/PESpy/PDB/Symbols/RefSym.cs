using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM* value;

        /// <inheritdoc cref="REFSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFSYM.usFill"/>
        public short usFill => value->usFill;

        //RefSym is the symbol type used by old ST symbols. These symbols have a hidden name after them not accounted for in their lengths
        public SymString name => GetName(null);

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            symbolAccessor ??= SymbolMemoryTracker.GetAccessor((long) value);

            //If we're NB11, there isn't a hidden name after us
            if (symbolAccessor is NB05SymbolAccessor a && a.CodeViewSig == CodeViewSig.NB11)
                return default;

            return SymType.ReadString(value, ((byte*) value) + reclen + sizeof(ushort), symbolAccessor);
        }

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short)  + //imod
            sizeof(short);   //usFill

        internal RefSym(REFSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFSYM, this, ViewKind.RefSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(sumName), sumName);
            s.WriteField(nameof(ibSym), ibSym);
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(usFill), usFill);
            s.WriteSymStringField(nameof(name), GetName(viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            var str = name;

            if (str.Length == 0)
                return Symbol.ToString();

            return str.ToString();
        }
    }
}
