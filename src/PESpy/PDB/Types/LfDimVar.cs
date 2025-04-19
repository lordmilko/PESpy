using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimVar"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimVar
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimVar* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int rank => value->rank;

        public CV_typ_t typ => value->typ;

        internal LfDimVar(lfDimVar* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
