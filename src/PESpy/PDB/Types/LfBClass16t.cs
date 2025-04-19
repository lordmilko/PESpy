using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBClass16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBClass_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        public CV_fldattr_t attr => value->attr;

        internal LfBClass16t(lfBClass_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
    }
}
