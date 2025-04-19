using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCobol1"/> structure.
    /// </summary>
    public readonly unsafe struct LfCobol1
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCobol1* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfCobol1(lfCobol1* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
