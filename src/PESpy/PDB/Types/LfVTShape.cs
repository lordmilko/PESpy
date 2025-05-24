using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVTShape"/> structure.
    /// </summary>
    public readonly unsafe struct LfVTShape
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVTShape* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfVTShape(lfVTShape* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read desc");
        }
    }
}
