using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfIndex_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfIndex16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfIndex_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //index

        internal LfIndex16t(lfIndex_16t* value)
        {
            this.value = value;
        }
    }
}
