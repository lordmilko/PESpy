using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRSLOTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AttrSlotSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRSLOTSYM* value;

        /// <inheritdoc cref="ATTRSLOTSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRSLOTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRSLOTSYM.iSlot"/>
        public int iSlot => value->iSlot;

        /// <inheritdoc cref="ATTRSLOTSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="ATTRSLOTSYM.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRSLOTSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //iSlot
            sizeof(int)    + //typind
            8;               //attr

        internal AttrSlotSym(ATTRSLOTSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

