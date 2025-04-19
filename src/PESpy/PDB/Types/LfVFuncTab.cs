using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncTab"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncTab
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncTab* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t type => value->type;

        internal LfVFuncTab(lfVFuncTab* value)
        {
            this.value = value;
        }
    }
}
