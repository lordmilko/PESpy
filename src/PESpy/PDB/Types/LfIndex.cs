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

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t index => value->index;

        internal LfIndex(lfIndex* value)
        {
            this.value = value;
        }
    }
}
