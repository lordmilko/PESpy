using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        internal LfDerived(lfDerived* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }
    }
}
