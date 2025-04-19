using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfIndex_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfIndex16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfIndex_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        internal LfIndex16t(lfIndex_16t* value)
        {
            this.value = value;
        }
    }
}
