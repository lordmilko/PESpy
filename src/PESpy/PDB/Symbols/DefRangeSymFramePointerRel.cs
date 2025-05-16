using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMFRAMEPOINTERREL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymFramePointerRel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMFRAMEPOINTERREL* value;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.offFramePointer"/>
        public CV_off32_t offFramePointer => value->offFramePointer;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.gaps"/>
        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //offFramePointer
            8;               //range

        internal DefRangeSymFramePointerRel(DEFRANGESYMFRAMEPOINTERREL* value)
        {
            this.value = value;
        }
    }
}

