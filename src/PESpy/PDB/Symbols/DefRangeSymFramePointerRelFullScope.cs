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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t offFramePointer => value->offFramePointer;

        internal DefRangeSymFramePointerRelFullScope(DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value)
        {
            this.value = value;
        }
    }
}

