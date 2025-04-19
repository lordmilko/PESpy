using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBitfield_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBitfield16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBitfield_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public byte length => value->length;

        public byte position => value->position;

        public CV_typ16_t type => value->type;

        internal LfBitfield16t(lfBitfield_16t* value)
        {
            this.value = value;
        }
    }
}
