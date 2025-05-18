using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfFieldList16t(lfFieldList_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
