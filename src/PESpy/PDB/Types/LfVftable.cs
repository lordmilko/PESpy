using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVftable"/> structure.
    /// </summary>
    public readonly unsafe struct LfVftable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVftable* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public TypOrEnumType baseVftable => new TypOrEnumType((byte*) value, value->baseVftable);

        public int offsetInObjectLayout => value->offsetInObjectLayout;

        public int len => value->len;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //type
            sizeof(int)    + //baseVftable
            sizeof(int)    + //offsetInObjectLayout
            sizeof(int);     //len

        internal LfVftable(lfVftable* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Names");
        }
    }
}
