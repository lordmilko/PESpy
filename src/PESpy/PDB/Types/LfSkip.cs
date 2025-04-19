using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSkip"/> structure.
    /// </summary>
    public readonly unsafe struct LfSkip
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSkip* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        internal LfSkip(lfSkip* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
