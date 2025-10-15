using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFMINIPDB"/> structure.
    /// </summary>
    public readonly unsafe struct RefMiniPdb : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFMINIPDB* value;

        /// <inheritdoc cref="REFMINIPDB.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFMINIPDB.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFMINIPDB.isectCoff"/>
        public int isectCoff => value->isectCoff;

        /// <inheritdoc cref="REFMINIPDB.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REFMINIPDB.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFMINIPDB.fLocal"/>
        public bool fLocal => value->fLocal;

        /// <inheritdoc cref="REFMINIPDB.fData"/>
        public bool fData => value->fData;

        /// <inheritdoc cref="REFMINIPDB.fUDT"/>
        public bool fUDT => value->fUDT;

        /// <inheritdoc cref="REFMINIPDB.fLabel"/>
        public bool fLabel => value->fLabel;

        /// <inheritdoc cref="REFMINIPDB.fConst"/>
        public bool fConst => value->fConst;

        /// <inheritdoc cref="REFMINIPDB.reserved"/>
        public short reserved => value->reserved;

        /// <inheritdoc cref="REFMINIPDB.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //isectCoff
            sizeof(int)    + //typind
            sizeof(short)  + //imod
            sizeof(short);   //data

        internal RefMiniPdb(REFMINIPDB* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFMINIPDB, this, ViewKind.RefMiniPdb, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            using (var bitField = s.WriteBitFields<long>())
            {
                bitField.WriteField(nameof(imod), imod, 1);
                bitField.WriteField(nameof(fLocal), fLocal, 1);
                bitField.WriteField(nameof(fData), fData, 1);
                bitField.WriteField(nameof(fUDT), fUDT, 1);
                bitField.WriteField(nameof(fLabel), fLabel, 1);
                bitField.WriteField(nameof(fConst), fConst, 1);
                bitField.WriteField(nameof(reserved), reserved, 11);
            }

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

