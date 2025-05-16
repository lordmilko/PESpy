using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYM"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYM* value;

        /// <inheritdoc cref="DEFRANGESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYM.program"/>
        public CV_uoff32_t program => value->program;

        /// <inheritdoc cref="DEFRANGESYM.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYM.gaps"/>
        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //program
            8;               //range

        internal DefRangeSym(DEFRANGESYM* value)
        {
            this.value = value;
        }
    }
}

