using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDefArg"/> structure.
    /// </summary>
    public readonly unsafe struct LfDefArg
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDefArg* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //type

        internal LfDefArg(lfDefArg* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read expr");
        }
    }
}
