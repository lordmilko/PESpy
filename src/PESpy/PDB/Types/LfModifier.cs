using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfModifier"/> structure.
    /// </summary>
    public readonly unsafe struct LfModifier
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfModifier* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public CV_modifier_t attr => value->attr;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            2;               //attr

        internal LfModifier(lfModifier* value)
        {
            this.value = value;
        }
    }
}
