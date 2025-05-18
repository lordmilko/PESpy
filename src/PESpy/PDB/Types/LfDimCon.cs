using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimCon"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimCon
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimCon* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t typ => value->typ;

        public short rank => value->rank;

        internal LfDimCon(lfDimCon* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
