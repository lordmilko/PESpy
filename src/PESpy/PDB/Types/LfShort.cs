using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfShort"/> structure.
    /// </summary>
    public readonly unsafe struct LfShort
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfShort* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short val => value->val;

        internal LfShort(lfShort* value)
        {
            this.value = value;
        }
    }
}
