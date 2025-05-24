using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFriendFcn"/> structure.
    /// </summary>
    public readonly unsafe struct LfFriendFcn
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFriendFcn* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t index => value->index;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //pad0
            sizeof(int);     //index

        internal LfFriendFcn(lfFriendFcn* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
