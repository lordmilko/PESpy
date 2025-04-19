using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethodList"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethodList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethodList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfMethodList(lfMethodList* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read mList");
        }
    }
}
