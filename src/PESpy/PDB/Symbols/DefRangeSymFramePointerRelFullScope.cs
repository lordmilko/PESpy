using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymFramePointerRelFullScope
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.offFramePointer"/>
        public CV_off32_t offFramePointer => value->offFramePointer;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //offFramePointer

        internal DefRangeSymFramePointerRelFullScope(DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value)
        {
            this.value = value;
        }
    }
}

