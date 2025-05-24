using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCobol0"/> structure.
    /// </summary>
    public readonly unsafe struct LfCobol0
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCobol0* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //type

        internal LfCobol0(lfCobol0* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
