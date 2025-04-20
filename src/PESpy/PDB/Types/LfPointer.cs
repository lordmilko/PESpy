using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->u.leaf;

        public CV_typ_t utype => value->u.utype;

        public lfPointer.lfPointerAttr attr => value->u.attr;

        public lfPointer.BaseInfo pbase => value->pbase;

        internal LfPointer(lfPointer* value)
        {
            this.value = value;
        }
    }
}
