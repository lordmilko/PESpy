using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        public CV_fldattr_t attr => value->attr;

        internal LfMember16t(lfMember_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
    }
}
