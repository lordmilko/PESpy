using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSkip_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfSkip16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSkip_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t type => value->type;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //type

        internal LfSkip16t(lfSkip_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
