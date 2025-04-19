using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFriendCls_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfFriendCls16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFriendCls_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        internal LfFriendCls16t(lfFriendCls_16t* value)
        {
            this.value = value;
        }
    }
}
