using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPreComp_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfPreComp16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPreComp_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short start => value->start;

        public short count => value->count;

        public int signature => value->signature;

        public SymString Name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //start
            sizeof(short)  + //count
            sizeof(int);     //signature

        internal LfPreComp16t(lfPreComp_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
