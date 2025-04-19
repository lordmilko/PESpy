using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimVar_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimVar16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimVar_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short rank => value->rank;

        public CV_typ16_t typ => value->typ;

        internal LfDimVar16t(lfDimVar_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read dim");
        }
    }
}
