using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfList"/> structure.
    /// </summary>
    public readonly unsafe struct LfList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfList* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfList(lfList* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
