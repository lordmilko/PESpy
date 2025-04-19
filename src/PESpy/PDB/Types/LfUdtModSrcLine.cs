using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUdtModSrcLine"/> structure.
    /// </summary>
    public readonly unsafe struct LfUdtModSrcLine
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUdtModSrcLine* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public CV_ItemId src => value->src;

        public int line => value->line;

        public short imod => value->imod;

        internal LfUdtModSrcLine(lfUdtModSrcLine* value)
        {
            this.value = value;
        }
    }
}
