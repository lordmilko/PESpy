using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    //Pointed to by LfMethod.mList?

    /// <summary>
    /// Represents the <see cref="lfMethodList"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethodList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethodList* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfMethodList(lfMethodList* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read mList");
        }
    }
}
