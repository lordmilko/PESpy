using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMemberModify"/> structure.
    /// </summary>
    public readonly unsafe struct LfMemberModify
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMemberModify* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public SymString Name => TypType.ReadString(value->Name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->Name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal LfMemberModify(lfMemberModify* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
