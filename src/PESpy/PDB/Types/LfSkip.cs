using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSkip"/> structure.
    /// </summary>
    public readonly unsafe struct LfSkip
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSkip* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        internal LfSkip(lfSkip* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
