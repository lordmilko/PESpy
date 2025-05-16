using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMSUBFIELD"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymSubField
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELD* value;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.program"/>
        public CV_uoff32_t program => value->program;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.gaps"/>
        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //program
            sizeof(uint)   + //offParent
            8;               //range

        internal DefRangeSymSubField(DEFRANGESYMSUBFIELD* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_LVAR_ADDR_GAP[] gaps");
        }
    }
}

