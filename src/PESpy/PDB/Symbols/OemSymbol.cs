using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OEMSYMBOL"/> structure.
    /// </summary>
    public readonly unsafe struct OemSymbol
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OEMSYMBOL* value;

        /// <inheritdoc cref="OEMSYMBOL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="OEMSYMBOL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="OEMSYMBOL.idOem"/>
        public Guid idOem => value->idOem;

        /// <inheritdoc cref="OEMSYMBOL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            16             + //idOem
            sizeof(int);     //typind

        internal OemSymbol(OEMSYMBOL* value)
        {
            this.value = value;
            Debug.Assert(false, "Read rgl");
        }
    }
}

