using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfModifierEx"/> structure.
    /// </summary>
    public readonly unsafe struct LfModifierEx
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfModifierEx* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(short);   //count

        internal LfModifierEx(lfModifierEx* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read mods");
        }
    }
}
