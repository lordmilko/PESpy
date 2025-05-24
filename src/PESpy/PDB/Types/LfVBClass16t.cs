using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVBClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVBClass16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVBClass_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public TypOrEnumType vbptr => new TypOrEnumType((byte*) value, value->vbptr);

        public CV_fldattr_t attr => value->attr;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            sizeof(short)  + //vbptr
            2;               //attr

        internal LfVBClass16t(lfVBClass_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbpoff");
        }
    }
}
