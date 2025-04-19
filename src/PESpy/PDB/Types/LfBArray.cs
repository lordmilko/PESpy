using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfBArray
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t utype => value->utype;

        internal LfBArray(lfBArray* value)
        {
            this.value = value;
        }
    }
}
