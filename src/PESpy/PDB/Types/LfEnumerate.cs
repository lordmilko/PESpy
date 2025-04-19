using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnumerate"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnumerate
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnumerate* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        internal LfEnumerate(lfEnumerate* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read value");
        }
    }
}
