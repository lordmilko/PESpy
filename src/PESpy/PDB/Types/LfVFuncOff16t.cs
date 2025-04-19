using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncOff_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncOff16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncOff_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t type => value->type;

        public CV_off32_t offset => value->offset;

        internal LfVFuncOff16t(lfVFuncOff_16t* value)
        {
            this.value = value;
        }
    }
}
