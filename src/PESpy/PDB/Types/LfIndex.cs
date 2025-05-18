using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfIndex"/> structure.
    /// </summary>
    public readonly unsafe struct LfIndex
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfIndex* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t index => value->index;

        internal LfIndex(lfIndex* value)
        {
            this.value = value;
        }
    }
}
