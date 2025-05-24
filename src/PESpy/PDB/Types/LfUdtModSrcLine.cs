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

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public CV_ItemId src => value->src;

        public int line => value->line;

        public short imod => value->imod;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //src
            sizeof(int)    + //line
            sizeof(short);   //imod

        internal LfUdtModSrcLine(lfUdtModSrcLine* value)
        {
            this.value = value;
        }
    }
}
