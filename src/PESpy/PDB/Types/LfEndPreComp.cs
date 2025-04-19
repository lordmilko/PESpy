using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEndPreComp"/> structure.
    /// </summary>
    public readonly unsafe struct LfEndPreComp
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEndPreComp* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int signature => value->signature;

        internal LfEndPreComp(lfEndPreComp* value)
        {
            this.value = value;
        }
    }
}
