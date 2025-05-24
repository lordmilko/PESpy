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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfRefSym(lfRefSym* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Sym");
        }
    }
}
