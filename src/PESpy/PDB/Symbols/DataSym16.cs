using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM16"/> structure.
    /// </summary>
    [DebuggerDisplay("{SymTypeProxy.DebuggerDisplay(this),nq}")] //For S_PUB16
    public readonly unsafe struct DataSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM16* value;

        /// <inheritdoc cref="DATASYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="DATASYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="DATASYM16.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short);   //typind

        internal DataSym16(DATASYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

