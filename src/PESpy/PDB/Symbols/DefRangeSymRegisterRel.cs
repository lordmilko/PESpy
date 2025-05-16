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

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.baseReg"/>
        public short baseReg => value->baseReg;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.spilledUdtMember"/>
        public bool spilledUdtMember => value->spilledUdtMember;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.padding"/>
        public short padding => value->padding;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.offsetParent"/>
        public short offsetParent => value->offsetParent;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.offBasePointer"/>
        public CV_off32_t offBasePointer => value->offBasePointer;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.gaps"/>
        public Span<CV_LVAR_ADDR_GAP> gaps => new Span<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(int)    + //CV_OFFSET_PARENT_LENGTH_LIMIT
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //baseReg
            sizeof(short)  + //flags
            sizeof(int)    + //offBasePointer
            8;               //range

        internal DefRangeSymRegisterRel(DEFRANGESYMREGISTERREL* value)
        {
            this.value = value;
        }
    }
}

