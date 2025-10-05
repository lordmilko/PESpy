using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OBJNAMESYM"/> structure.
    /// </summary>
    public readonly unsafe struct ObjNameSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OBJNAMESYM* value;

        /// <inheritdoc cref="OBJNAMESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="OBJNAMESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="OBJNAMESYM.signature"/>
        public int signature => value->signature;

        /// <inheritdoc cref="OBJNAMESYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //signature

        internal ObjNameSym(OBJNAMESYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

