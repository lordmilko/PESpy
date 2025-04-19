using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfRefSym"/> structure.
    /// </summary>
    public readonly unsafe struct LfRefSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfRefSym* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        internal LfRefSym(lfRefSym* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Sym");
        }
    }
}
