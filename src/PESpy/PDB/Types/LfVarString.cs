using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVarString"/> structure.
    /// </summary>
    public readonly unsafe struct LfVarString
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVarString* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short len => value->len;

        internal LfVarString(lfVarString* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read value");
        }
    }
}
