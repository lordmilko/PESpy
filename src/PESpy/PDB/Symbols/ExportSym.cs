using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="EXPORTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ExportSym
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

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

