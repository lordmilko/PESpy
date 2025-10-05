using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFuncId"/> structure.
    /// </summary>
    public readonly unsafe struct LfFuncId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFuncId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType scopeId => new TypOrEnumType((byte*) value, value->scopeId);

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public SymString Name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //scopeId
            sizeof(int);     //type

        internal LfFuncId(lfFuncId* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
