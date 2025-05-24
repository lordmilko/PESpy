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

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public CV_off32_t offset => value->offset;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //type
            sizeof(int);     //offset

        internal LfVFuncOff16t(lfVFuncOff_16t* value)
        {
            this.value = value;
        }
    }
}
