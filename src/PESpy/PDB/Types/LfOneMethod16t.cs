using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOneMethod_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfOneMethod16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOneMethod_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ16_t index => value->index;

        //fixed int vbaseoff[1]

        internal LfOneMethod16t(lfOneMethod_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbaseoff");
        }
    }
}
