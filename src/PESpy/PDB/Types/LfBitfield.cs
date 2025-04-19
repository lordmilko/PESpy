using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBitfield"/> structure.
    /// </summary>
    public readonly unsafe struct LfBitfield
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBitfield* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t type => value->type;

        public byte length => value->length;

        public byte position => value->position;

        internal LfBitfield(lfBitfield* value)
        {
            this.value = value;
        }
    }
}
