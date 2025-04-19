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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t program => value->program;
        public CV_uoff32_t offParent => value->offParent;
        public CV_LVAR_ADDR_RANGE range => value->range;

        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal DefRangeSymSubField(DEFRANGESYMSUBFIELD* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_LVAR_ADDR_GAP[] gaps");
        }
    }
}

