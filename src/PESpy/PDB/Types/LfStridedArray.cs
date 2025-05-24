using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfStridedArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfStridedArray
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfStridedArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public TypOrEnumType idxtype => new TypOrEnumType((byte*) value, value->idxtype);

        public int stride => value->stride;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int)    + //idxtype
            sizeof(int);     //stride

        internal LfStridedArray(lfStridedArray* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
