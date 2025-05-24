using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANTYPREF"/> structure.
    /// </summary>
    public readonly unsafe struct ManTypRef
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANTYPREF* value;

        /// <inheritdoc cref="MANTYPREF.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANTYPREF.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANTYPREF.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //typind

        internal ManTypRef(MANTYPREF* value)
        {
            this.value = value;
        }
    }
}

