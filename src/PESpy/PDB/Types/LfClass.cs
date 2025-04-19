using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfClass* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public CV_typ_t field => value->field;

        public CV_typ_t derived => value->derived;

        public CV_typ_t vshape => value->vshape;

        internal LfClass(lfClass* value)
        {
            this.value = value;

            TypType.AssertMissing(false, "Read data");
        }
    }
}
