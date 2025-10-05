using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UNAMESPACE"/> structure.
    /// </summary>
    public readonly unsafe struct UNameSpace
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UNAMESPACE* value;

        /// <inheritdoc cref="UNAMESPACE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="UNAMESPACE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="UNAMESPACE.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal UNameSpace(UNAMESPACE* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

