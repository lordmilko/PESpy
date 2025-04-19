using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal16"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal16* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short val => value->val;

        internal LfReal16(lfReal16* value)
        {
            this.value = value;
        }
    }
}
