using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        internal LfDerived(lfDerived* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }
    }
}
