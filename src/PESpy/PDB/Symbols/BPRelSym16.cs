using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM16* value;

        /// <inheritdoc cref="BPRELSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BPRELSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BPRELSYM16.off"/>
        public CV_off16_t off => value->off;

        /// <inheritdoc cref="BPRELSYM16.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="BPRELSYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //off
            sizeof(short);   //typind

        internal BPRelSym16(BPRELSYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

