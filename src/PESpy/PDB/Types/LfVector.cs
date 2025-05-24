using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVector"/> structure.
    /// </summary>
    public readonly unsafe struct LfVector
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVector* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int);     //count

        internal LfVector(lfVector* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
