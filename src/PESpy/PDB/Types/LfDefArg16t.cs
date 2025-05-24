using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDefArg_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDefArg16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDefArg_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //type

        internal LfDefArg16t(lfDefArg_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read expr");
        }
    }
}
