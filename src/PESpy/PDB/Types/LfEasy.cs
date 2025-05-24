using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEasy"/> structure.
    /// </summary>
    public readonly unsafe struct LfEasy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEasy* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int StructSize =
            sizeof(ushort);  //leaf

        internal LfEasy(lfEasy* value)
        {
            this.value = value;
        }
    }
}
