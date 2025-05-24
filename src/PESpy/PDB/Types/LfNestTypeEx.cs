using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfNestTypeEx"/> structure.
    /// </summary>
    public readonly unsafe struct LfNestTypeEx
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfNestTypeEx* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal LfNestTypeEx(lfNestTypeEx* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
