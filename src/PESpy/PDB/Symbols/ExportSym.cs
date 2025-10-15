using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="EXPORTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ExportSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly EXPORTSYM* value;

        /// <inheritdoc cref="EXPORTSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="EXPORTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="EXPORTSYM.ordinal"/>
        public short ordinal => value->ordinal;

        /// <inheritdoc cref="EXPORTSYM.fConstant"/>
        public bool fConstant => value->fConstant;

        /// <inheritdoc cref="EXPORTSYM.fData"/>
        public bool fData => value->fData;

        /// <inheritdoc cref="EXPORTSYM.fPrivate"/>
        public bool fPrivate => value->fPrivate;

        /// <inheritdoc cref="EXPORTSYM.fNoName"/>
        public bool fNoName => value->fNoName;

        /// <inheritdoc cref="EXPORTSYM.fOrdinal"/>
        public bool fOrdinal => value->fOrdinal;

        /// <inheritdoc cref="EXPORTSYM.fForwarder"/>
        public bool fForwarder => value->fForwarder;

        /// <inheritdoc cref="EXPORTSYM.reserved"/>
        public short reserved => value->reserved;

        /// <inheritdoc cref="EXPORTSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //ordinal
            sizeof(short);   //data

        internal ExportSym(EXPORTSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.EXPORTSYM, this, ViewKind.ExportSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(ordinal), ordinal);

            using (var bitField = s.WriteBitFields<short>())
            {
                bitField.WriteField(nameof(fConstant), fConstant, 1);
                bitField.WriteField(nameof(fData), fData, 1);
                bitField.WriteField(nameof(fPrivate), fPrivate, 1);
                bitField.WriteField(nameof(fNoName), fNoName, 1);
                bitField.WriteField(nameof(fOrdinal), fOrdinal, 1);
                bitField.WriteField(nameof(fForwarder), fForwarder, 1);
                bitField.WriteField(nameof(reserved), reserved, 10);
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
