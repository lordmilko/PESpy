using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FILESTATICSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FileStaticSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FILESTATICSYM* value;

        /// <inheritdoc cref="FILESTATICSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FILESTATICSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FILESTATICSYM.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="FILESTATICSYM.modOffset"/>
        public CV_uoff32_t modOffset => value->modOffset;

        /// <inheritdoc cref="FILESTATICSYM.flags"/>
        public CV_LVARFLAGS flags => value->flags;

        /// <inheritdoc cref="FILESTATICSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(uint)   + //modOffset
            2;               //flags

        internal FileStaticSym(FILESTATICSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

