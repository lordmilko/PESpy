using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfChar"/> structure.
    /// </summary>
    public readonly unsafe struct LfChar
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfChar* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public sbyte val => value->val;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(sbyte);   //val

        internal LfChar(lfChar* value)
        {
            this.value = value;
        }
    }
}
