using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal32"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal32* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public float val => value->val;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(float);   //val

        internal LfReal32(lfReal32* value)
        {
            this.value = value;
        }
    }
}
