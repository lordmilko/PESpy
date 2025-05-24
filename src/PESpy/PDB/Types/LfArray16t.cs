using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArray_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfArray16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArray_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t elemtype => value->elemtype;

        public CV_typ16_t idxtype => value->idxtype;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //elemtype
            sizeof(short);   //idxtype

        internal LfArray16t(lfArray_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
