using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfModifier_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfModifier16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfModifier_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_modifier_t attr => value->attr;

        public CV_typ16_t type => value->type;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(short);   //type

        internal LfModifier16t(lfModifier_16t* value)
        {
            this.value = value;
        }
    }
}
