using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM* raw;

        /// <inheritdoc cref="CONSTSYM.reclen"/>
        public ushort reclen => raw->reclen;

        /// <inheritdoc cref="CONSTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => raw->rectyp;

        /// <inheritdoc cref="CONSTSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) raw, raw->typind);

        /// <inheritdoc cref="CONSTSYM.value"/>
        public short value => raw->value;

        //Note: according to dumpsym7.cpp!C7ConSym, name does not actually contain name; you have to skip over a type encoded value indicated by "value"

        /// <inheritdoc cref="CONSTSYM.name"/>
        public SymString name => SymType.ReadString(raw, raw->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(raw, raw->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short);   //value

        internal ConstSym(CONSTSYM* value)
        {
            this.raw = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

