using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVBClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfVBClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVBClass* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public TypOrEnumType vbptr => new TypOrEnumType((byte*) value, value->vbptr);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int)    + //index
            sizeof(int);     //vbptr

        internal LfVBClass(lfVBClass* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbpoff");
        }
    }
}
