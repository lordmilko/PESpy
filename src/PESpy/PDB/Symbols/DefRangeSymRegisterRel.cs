using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMREGISTERREL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymRegisterRel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMREGISTERREL* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public short baseReg => value->baseReg;

        public bool spilledUdtMember => value->spilledUdtMember;

        public short padding => value->padding;

        public short offsetParent => value->offsetParent;

        public CV_off32_t offBasePointer => value->offBasePointer;

        public CV_LVAR_ADDR_RANGE range => value->range;

        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal DefRangeSymRegisterRel(DEFRANGESYMREGISTERREL* value)
        {
            this.value = value;
        }
    }
}

