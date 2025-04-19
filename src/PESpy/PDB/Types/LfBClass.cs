using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfBClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBClass* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        internal LfBClass(lfBClass* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
    }
}
