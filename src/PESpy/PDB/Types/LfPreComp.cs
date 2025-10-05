using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPreComp"/> structure.
    /// </summary>
    public readonly unsafe struct LfPreComp
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPreComp* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int start => value->start;

        public int count => value->count;

        public int signature => value->signature;

        public SymString Name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //start
            sizeof(int)    + //count
            sizeof(int);     //signature

        internal LfPreComp(lfPreComp* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
