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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //pad0
            sizeof(int);     //index

        internal LfFriendCls(lfFriendCls* value)
        {
            this.value = value;
        }
    }
}
