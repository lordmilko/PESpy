using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfDerived16t(lfDerived_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }
    }
}
