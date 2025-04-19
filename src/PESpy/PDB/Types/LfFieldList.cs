using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfFieldList(lfFieldList* value)
        {
            this.value = value;
            Debug.Assert(false, "Read data");
        }
    }
}
