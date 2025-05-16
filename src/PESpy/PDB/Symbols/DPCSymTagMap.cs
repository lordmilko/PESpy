using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DPCSYMTAGMAP"/> structure.
    /// </summary>
    public readonly unsafe struct DPCSymTagMap
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DPCSYMTAGMAP* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        internal DPCSymTagMap(DPCSYMTAGMAP* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_DPC_SYM_TAG_MAP_ENTRY[] mapEntries");
        }
    }
}

