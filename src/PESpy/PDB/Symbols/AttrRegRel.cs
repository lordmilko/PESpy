using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGREL"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegRel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGREL* value;

        /// <inheritdoc cref="ATTRREGREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRREGREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRREGREL.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="ATTRREGREL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="ATTRREGREL.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="ATTRREGREL.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRREGREL.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(int)    + //typind
            sizeof(short)  + //reg
            8;               //attr

        internal AttrRegRel(ATTRREGREL* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

