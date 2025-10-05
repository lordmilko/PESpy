using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfStringId"/> structure.
    /// </summary>
    public readonly unsafe struct LfStringId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfStringId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType id => new TypOrEnumType((byte*) value, value->id);

        public SymString Name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //id

        internal LfStringId(lfStringId* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
