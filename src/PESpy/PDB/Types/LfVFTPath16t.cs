using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFTPath_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFTPath16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFTPath_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfVFTPath16t(lfVFTPath_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read base");
        }
    }
}
