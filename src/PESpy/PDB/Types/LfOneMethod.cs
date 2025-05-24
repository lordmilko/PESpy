using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOneMethod"/> structure.
    /// </summary>
    public readonly unsafe struct LfOneMethod
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOneMethod* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public CV_typ_t index => value->index;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal LfOneMethod(lfOneMethod* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbaseoff");
        }
    }
}
