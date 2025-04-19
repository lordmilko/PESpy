using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMSUBFIELDREGISTER"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymSubfieldRegister
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELDREGISTER* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public short reg => value->reg;

        public CV_RANGEATTR attr => value->attr;

        public CV_uoff32_t offParent => value->offParent;

        public CV_uoff32_t padding => value->padding;

        public CV_LVAR_ADDR_RANGE range => value->range;

        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal DefRangeSymSubfieldRegister(DEFRANGESYMSUBFIELDREGISTER* value)
        {
            this.value = value;
        }
    }
}

