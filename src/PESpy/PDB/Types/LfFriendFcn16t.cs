using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFriendFcn_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfFriendFcn16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFriendFcn_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfFriendFcn16t(lfFriendFcn_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
