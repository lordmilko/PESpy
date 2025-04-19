using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->u.leaf;

        
    }
}
