using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        internal LfMember(lfMember* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
    }
}
