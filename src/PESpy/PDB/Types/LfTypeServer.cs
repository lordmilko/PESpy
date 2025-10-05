using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfTypeServer"/> structure.
    /// </summary>
    public readonly unsafe struct LfTypeServer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfTypeServer* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int signature => value->signature;

        public int age => value->age;

        public SymString Name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //signature
            sizeof(int);     //age

        internal LfTypeServer(lfTypeServer* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
