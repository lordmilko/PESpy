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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

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
