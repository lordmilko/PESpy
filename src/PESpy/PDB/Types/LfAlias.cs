using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfAlias"/> structure.
    /// </summary>
    public readonly unsafe struct LfAlias
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfAlias* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t utype => value->utype;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //utype

        internal LfAlias(lfAlias* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
