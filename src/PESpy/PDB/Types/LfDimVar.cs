using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimVar"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimVar
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimVar* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public int rank => value->rank;

        public TypOrEnumType typ => new TypOrEnumType((byte*) value, value->typ);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //rank
            sizeof(int);     //typ

        internal LfDimVar(lfDimVar* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
