using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM32_16t* value;

        /// <inheritdoc cref="BPRELSYM32_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BPRELSYM32_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BPRELSYM32_16t.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="BPRELSYM32_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="BPRELSYM32_16t.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short);   //typind

        internal BPRelSym3216t(BPRELSYM32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

