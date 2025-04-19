using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfArray
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t elemtype => value->elemtype;

        public CV_typ_t idxtype => value->idxtype;

        internal LfArray(lfArray* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
