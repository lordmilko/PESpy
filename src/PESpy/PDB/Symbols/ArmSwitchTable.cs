using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ARMSWITCHTABLE"/> structure.
    /// </summary>
    public readonly unsafe struct ArmSwitchTable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ARMSWITCHTABLE* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t offsetBase => value->offsetBase;

        public short sectBase => value->sectBase;

        public short switchType => value->switchType;

        public CV_uoff32_t offsetBranch => value->offsetBranch;

        public CV_uoff32_t offsetTable => value->offsetTable;

        public short sectBranch => value->sectBranch;

        public short sectTable => value->sectTable;

        public int cEntries => value->cEntries;

        internal ArmSwitchTable(ARMSWITCHTABLE* value)
        {
            this.value = value;
        }
    }
}

