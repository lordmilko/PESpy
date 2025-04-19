using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimCon"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimCon
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimCon* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t typ => value->typ;

        public short rank => value->rank;

        internal LfDimCon(lfDimCon* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
