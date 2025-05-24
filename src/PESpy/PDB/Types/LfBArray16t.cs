using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBArray_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBArray16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBArray_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t utype => value->utype;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //utype

        internal LfBArray16t(lfBArray_16t* value)
        {
            this.value = value;
        }
    }
}
