using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UDTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct UdtSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UDTSYM* value;

        /// <inheritdoc cref="UDTSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="UDTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="UDTSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="UDTSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //typind

        internal UdtSym(UDTSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

