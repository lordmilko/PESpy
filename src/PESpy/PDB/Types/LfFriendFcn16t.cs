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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //index

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
