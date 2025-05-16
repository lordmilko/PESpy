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

        /// <inheritdoc cref="DEFRANGESYMHLSL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMHLSL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMHLSL.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DEFRANGESYMHLSL.regIndices"/>
        public short regIndices => value->regIndices;

        /// <inheritdoc cref="DEFRANGESYMHLSL.spilledUdtMember"/>
        public bool spilledUdtMember => value->spilledUdtMember;

        /// <inheritdoc cref="DEFRANGESYMHLSL.memorySpace"/>
        public short memorySpace => value->memorySpace;

        /// <inheritdoc cref="DEFRANGESYMHLSL.padding"/>
        public short padding => value->padding;

        /// <inheritdoc cref="DEFRANGESYMHLSL.offsetParent"/>
        public short offsetParent => value->offsetParent;

        /// <inheritdoc cref="DEFRANGESYMHLSL.sizeInParent"/>
        public short sizeInParent => value->sizeInParent;

        /// <inheritdoc cref="DEFRANGESYMHLSL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //regType
            sizeof(short)  + //data1
            sizeof(short)  + //offsetParent
            sizeof(short)  + //sizeInParent
            8;               //range

        internal DefRangeSymHLSL(DEFRANGESYMHLSL* value)
        {
            this.value = value;
            Debug.Assert(false, "Use macros in DEFRANGESYMHLSL to read gaps, data and multi-dimensional offsets of variable locations in register space");
        }
    }
}

