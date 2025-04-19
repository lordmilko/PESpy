using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUnion"/> structure.
    /// </summary>
    public readonly unsafe struct LfUnion
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUnion* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public CV_typ_t field => value->field;

        internal LfUnion(lfUnion* value)
        {
            this.value = value;

            TypType.AssertMissing(false, "Read data");
        }
    }
}
