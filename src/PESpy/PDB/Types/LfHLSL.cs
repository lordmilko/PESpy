using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct LfHLSL
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfHLSL* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t subtype => value->subtype;

        public short kind => value->kind;

        public short numprops => value->numprops;

        public short unused => value->unused;

        public short propdata => value->propdata;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //subtype
            sizeof(short)  + //kind
            sizeof(short);   //propdata

        internal LfHLSL(lfHLSL* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
