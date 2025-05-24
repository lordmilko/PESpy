using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short cvOEM => value->cvOEM;

        public short recOEM => value->recOEM;

        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //cvOEM
            sizeof(short)  + //recOEM
            sizeof(short);   //count

        internal LfOEM16t(lfOEM_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }
    }
}
