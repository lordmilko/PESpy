using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUdtSrcLine"/> structure.
    /// </summary>
    public readonly unsafe struct LfUdtSrcLine
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUdtSrcLine* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public CV_ItemId src => value->src;

        public int line => value->line;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //src
            sizeof(int);     //line

        internal LfUdtSrcLine(lfUdtSrcLine* value)
        {
            this.value = value;
        }
    }
}
