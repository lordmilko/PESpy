using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LOCALSYM"/> structure.
    /// </summary>
    public readonly unsafe struct LocalSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LOCALSYM* value;

        /// <inheritdoc cref="LOCALSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LOCALSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LOCALSYM.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="LOCALSYM.flags"/>
        public CV_LVARFLAGS flags => value->flags;

        /// <inheritdoc cref="LOCALSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            2;               //flags

        internal LocalSym(LOCALSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

