using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUnion_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfUnion16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUnion_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_typ16_t field => value->field;

        public CV_prop_t property => value->property;

        internal LfUnion16t(lfUnion_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
