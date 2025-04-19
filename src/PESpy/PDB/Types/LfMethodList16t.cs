using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethodList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethodList16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethodList_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        //fixed byte mList[1]

        internal LfMethodList16t(lfMethodList_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read mList");
        }
    }
}
