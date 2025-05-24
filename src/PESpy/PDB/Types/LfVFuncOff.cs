using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncOff"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncOff
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncOff* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t type => value->type;

        public CV_off32_t offset => value->offset;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //pad0
            sizeof(int)    + //type
            sizeof(int);     //offset

        internal LfVFuncOff(lfVFuncOff* value)
        {
            this.value = value;
        }
    }
}
