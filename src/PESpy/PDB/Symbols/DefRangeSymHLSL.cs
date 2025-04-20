using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymHLSL
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMHLSL* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public short regType => value->regType;

        public short regIndices => value->regIndices;

        public bool spilledUdtMember => value->spilledUdtMember;

        public short memorySpace => value->memorySpace;

        public short padding => value->padding;

        public short offsetParent => value->offsetParent;

        public short sizeInParent => value->sizeInParent;

        public CV_LVAR_ADDR_RANGE range => value->range;

        internal DefRangeSymHLSL(DEFRANGESYMHLSL* value)
        {
            this.value = value;
            Debug.Assert(false, "Use macros in DEFRANGESYMHLSL to read gaps, data and multi-dimensional offsets of variable locations in register space");
        }
    }
}

