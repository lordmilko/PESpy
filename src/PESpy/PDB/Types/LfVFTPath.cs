using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFTPath"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFTPath
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFTPath* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        internal LfVFTPath(lfVFTPath* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read base");
        }
    }
}
