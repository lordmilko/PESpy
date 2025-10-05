using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMERELSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameRelSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMERELSYM* value;

        /// <inheritdoc cref="FRAMERELSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FRAMERELSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FRAMERELSYM.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="FRAMERELSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="FRAMERELSYM.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="FRAMERELSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(int)    + //typind
            8;               //attr

        internal FrameRelSym(FRAMERELSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

