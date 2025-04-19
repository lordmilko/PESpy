using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncTab_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncTab16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncTab_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t type => value->type;

        internal LfVFuncTab16t(lfVFuncTab_16t* value)
        {
            this.value = value;
        }
    }
}
