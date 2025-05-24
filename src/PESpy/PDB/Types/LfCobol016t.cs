using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCobol0_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfCobol016t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCobol0_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //type

        internal LfCobol016t(lfCobol0_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
