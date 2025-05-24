using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimArray
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        public TypOrEnumType diminfo => new TypOrEnumType((byte*) value, value->diminfo);

        public FixedUtf8String Name => TypType.ReadString(value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //utype
            sizeof(int);     //diminfo

        internal LfDimArray(lfDimArray* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
