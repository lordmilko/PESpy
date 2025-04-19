using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfList"/> structure.
    /// </summary>
    public readonly unsafe struct LfList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfList(lfList* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
