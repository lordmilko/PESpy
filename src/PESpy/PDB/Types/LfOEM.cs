using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short cvOEM => value->cvOEM;

        public short recOEM => value->recOEM;

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //cvOEM
            sizeof(short)  + //recOEM
            sizeof(int);     //count

        internal LfOEM(lfOEM* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }
    }
}
