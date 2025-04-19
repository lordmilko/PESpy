using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMemberModify"/> structure.
    /// </summary>
    public readonly unsafe struct LfMemberModify
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMemberModify* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfMemberModify(lfMemberModify* value)
        {
            this.value = value;
        }
    }
}
