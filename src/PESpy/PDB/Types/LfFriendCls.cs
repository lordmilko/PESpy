using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFriendCls"/> structure.
    /// </summary>
    public readonly unsafe struct LfFriendCls
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFriendCls* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t index => value->index;

        internal LfFriendCls(lfFriendCls* value)
        {
            this.value = value;
        }
    }
}
