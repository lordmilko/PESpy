using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfClass16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfClass_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public CV_prop_t property => value->property;

        public TypOrEnumType derived => new TypOrEnumType((byte*) value, value->derived);

        public TypOrEnumType vshape => new TypOrEnumType((byte*) value, value->vshape);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(short)  + //field
            2              + //property
            sizeof(short)  + //derived
            sizeof(short);   //vshape

        internal LfClass16t(lfClass_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }
    }
}
