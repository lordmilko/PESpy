using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SLOTSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct SlotSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SLOTSYM32* value;

        /// <inheritdoc cref="SLOTSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SLOTSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SLOTSYM32.iSlot"/>
        public int iSlot => value->iSlot;

        /// <inheritdoc cref="SLOTSYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="SLOTSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //iSlot
            sizeof(int);     //typind

        internal SlotSym32(SLOTSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

