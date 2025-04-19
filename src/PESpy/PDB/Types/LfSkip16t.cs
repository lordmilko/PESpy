using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSkip_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfSkip16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSkip_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t type => value->type;

        internal LfSkip16t(lfSkip_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
