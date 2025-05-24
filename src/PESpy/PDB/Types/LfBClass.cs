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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal LfBClass(lfBClass* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
    }
}
