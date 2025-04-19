using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfULong"/> structure.
    /// </summary>
    public readonly unsafe struct LfULong
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfULong* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int val => value->val;

        internal LfULong(lfULong* value)
        {
            this.value = value;
        }
    }
}
