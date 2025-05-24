using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal64"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal64
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal64* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public double val => value->val;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(double);  //val

        internal LfReal64(lfReal64* value)
        {
            this.value = value;
        }
    }
}
