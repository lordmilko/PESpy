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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t offFramePointer => value->offFramePointer;

        public CV_LVAR_ADDR_RANGE range => value->range;

        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        internal DefRangeSymFramePointerRel(DEFRANGESYMFRAMEPOINTERREL* value)
        {
            this.value = value;
        }
    }
}

