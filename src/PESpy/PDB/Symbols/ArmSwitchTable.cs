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

        /// <inheritdoc cref="ARMSWITCHTABLE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ARMSWITCHTABLE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetBase"/>
        public CV_uoff32_t offsetBase => value->offsetBase;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectBase"/>
        public short sectBase => value->sectBase;

        /// <inheritdoc cref="ARMSWITCHTABLE.switchType"/>
        public short switchType => value->switchType;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetBranch"/>
        public CV_uoff32_t offsetBranch => value->offsetBranch;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetTable"/>
        public CV_uoff32_t offsetTable => value->offsetTable;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectBranch"/>
        public short sectBranch => value->sectBranch;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectTable"/>
        public short sectTable => value->sectTable;

        /// <inheritdoc cref="ARMSWITCHTABLE.cEntries"/>
        public int cEntries => value->cEntries;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //offsetBase
            sizeof(short)  + //sectBase
            sizeof(short)  + //switchType
            sizeof(uint)   + //offsetBranch
            sizeof(uint)   + //offsetTable
            sizeof(short)  + //sectBranch
            sizeof(short)  + //sectTable
            sizeof(int);     //cEntries

        internal ArmSwitchTable(ARMSWITCHTABLE* value)
        {
            this.value = value;
        }
    }
}

