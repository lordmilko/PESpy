using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfLong"/> structure.
    /// </summary>
    public readonly unsafe struct LfLong
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfLong* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int val => value->val;

        internal LfLong(lfLong* value)
        {
            this.value = value;
        }
    }
}
