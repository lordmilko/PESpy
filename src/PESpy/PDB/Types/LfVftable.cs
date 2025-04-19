using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVftable"/> structure.
    /// </summary>
    public readonly unsafe struct LfVftable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVftable* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public CV_typ_t baseVftable => value->baseVftable;

        public int offsetInObjectLayout => value->offsetInObjectLayout;

        public int len => value->len;

        internal LfVftable(lfVftable* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read Names");
        }
    }
}
