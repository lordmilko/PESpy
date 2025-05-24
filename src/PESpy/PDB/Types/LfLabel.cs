using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfLabel"/> structure.
    /// </summary>
    public readonly unsafe struct LfLabel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfLabel* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short mode => value->mode;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //mode

        internal LfLabel(lfLabel* value)
        {
            this.value = value;
        }
    }
}
