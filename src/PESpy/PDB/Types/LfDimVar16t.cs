using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimVar_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimVar16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimVar_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short rank => value->rank;

        public TypOrEnumType typ => new TypOrEnumType((byte*) value, value->typ);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //rank
            sizeof(short);   //typ

        internal LfDimVar16t(lfDimVar_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
