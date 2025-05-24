using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethod_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethod16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethod_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType mList => new TypOrEnumType((byte*) value, value->mList);

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(short);   //mList

        internal LfMethod16t(lfMethod_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
