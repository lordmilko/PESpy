using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMREGISTER"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymRegister
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMREGISTER* value;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.reg"/>
        public short reg => value->reg;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.attr"/>
        public CV_RANGEATTR attr => value->attr;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.gaps"/>
        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //reg
            2              + //attr
            8;               //range

        internal DefRangeSymRegister(DEFRANGESYMREGISTER* value)
        {
            this.value = value;
        }
    }
}

