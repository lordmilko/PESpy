using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUShort"/> structure.
    /// </summary>
    public readonly unsafe struct LfUShort
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUShort* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short val => value->val;

        internal LfUShort(lfUShort* value)
        {
            this.value = value;
        }
    }
}
