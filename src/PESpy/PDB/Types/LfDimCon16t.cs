using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimCon_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimCon16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimCon_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short rank => value->rank;

        public CV_typ16_t typ => value->typ;

        internal LfDimCon16t(lfDimCon_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
